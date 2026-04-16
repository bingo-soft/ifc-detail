using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace Bingosoft.Net.IfcDetail;

internal enum StepValueKind
{
    Null,
    Omitted,
    String,
    Number,
    Enum,
    Reference,
    List,
    Raw
}

internal readonly record struct StepValue(
    StepValueKind Kind,
    string Text,
    double Number,
    int ReferenceId,
    int ListStart,
    int ListCount)
{
    public static StepValue Null() => new(StepValueKind.Null, string.Empty, 0, 0, 0, 0);
    public static StepValue Omitted() => new(StepValueKind.Omitted, string.Empty, 0, 0, 0, 0);
    public static StepValue String(string value) => new(StepValueKind.String, value, 0, 0, 0, 0);
    public static StepValue FromNumber(double value) => new(StepValueKind.Number, string.Empty, value, 0, 0, 0);
    public static StepValue Enum(string value) => new(StepValueKind.Enum, value, 0, 0, 0, 0);
    public static StepValue Reference(int value) => new(StepValueKind.Reference, string.Empty, 0, value, 0, 0);
    public static StepValue List(int start, int count) => new(StepValueKind.List, string.Empty, 0, 0, start, count);
    public static StepValue Raw(string value) => new(StepValueKind.Raw, value, 0, 0, 0, 0);
}

internal sealed class StepEntityTable(
    int[] entityIds,
    string[] entityTypes,
    int[] argOffsets,
    int[] argCounts,
    int[] argValueIndices,
    StepValue[] values,
    int[] listItems,
    Dictionary<int, int> entityIndexById)
{
    public int Count => entityIds.Length;

    public int GetEntityId(int entityIndex) => entityIds[entityIndex];
    public string GetEntityType(int entityIndex) => entityTypes[entityIndex];

    public bool TryGetEntityIndex(int entityId, out int entityIndex) => entityIndexById.TryGetValue(entityId, out entityIndex);

    public int GetArgumentCount(int entityIndex) => argCounts[entityIndex];

    public StepValue GetArgumentValue(int entityIndex, int argumentIndex)
    {
        var count = argCounts[entityIndex];
        if (argumentIndex < 0 || argumentIndex >= count)
        {
            return StepValue.Omitted();
        }

        var start = argOffsets[entityIndex];
        var valueIndex = argValueIndices[start + argumentIndex];
        return values[valueIndex];
    }

    public void ForEachListItem(StepValue value, Action<StepValue> action)
    {
        if (value.Kind != StepValueKind.List || value.ListCount == 0)
        {
            return;
        }

        var end = value.ListStart + value.ListCount;
        for (var i = value.ListStart; i < end; i++)
        {
            var valueIndex = listItems[i];
            action(values[valueIndex]);
        }
    }

    public void ForEachReferenceInEntity(int entityIndex, Action<int> action)
    {
        var count = argCounts[entityIndex];
        var start = argOffsets[entityIndex];

        for (var i = 0; i < count; i++)
        {
            var valueIndex = argValueIndices[start + i];
            ForEachReference(values[valueIndex], action);
        }
    }

    private void ForEachReference(StepValue value, Action<int> action)
    {
        switch (value.Kind)
        {
            case StepValueKind.Reference:
                action(value.ReferenceId);
                break;
            case StepValueKind.List:
                ForEachListItem(value, item => ForEachReference(item, action));
                break;
        }
    }
}

internal sealed class EntityAdjacencyIndex
{
    public EntityAdjacencyIndex(StepEntityTable table)
    {
        var offsets = new int[table.Count + 1];
        var edges = new List<int>(table.Count * 2);

        for (var sourceIndex = 0; sourceIndex < table.Count; sourceIndex++)
        {
            offsets[sourceIndex] = edges.Count;

            var uniqueTargets = new HashSet<int>();
            table.ForEachReferenceInEntity(sourceIndex, entityId =>
            {
                if (!table.TryGetEntityIndex(entityId, out var targetIndex))
                {
                    return;
                }

                if (uniqueTargets.Add(targetIndex))
                {
                    edges.Add(targetIndex);
                }
            });
        }

        offsets[table.Count] = edges.Count;

        Offsets = offsets;
        Edges = edges.ToArray();
    }

    public int[] Offsets { get; }

    public int[] Edges { get; }
}

internal sealed class FastIfcDataModel
{
    private readonly Dictionary<string, List<int>> _entitiesByType;
    private readonly Dictionary<int, List<int>> _materialLayerToLayerSets;
    private readonly string[] _globalIdCache;

    public FastIfcDataModel(StepEntityTable table)
    {
        Table = table;

        _entitiesByType = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        _globalIdCache = new string[table.Count];


        for (var i = 0; i < table.Count; i++)
        {
            var entityType = table.GetEntityType(i);
            if (!_entitiesByType.TryGetValue(entityType, out var entities))
            {
                entities = [];
                _entitiesByType[entityType] = entities;
            }

            entities.Add(i);
        }

        _materialLayerToLayerSets = BuildLayerToLayerSetMap();
    }

    public StepEntityTable Table { get; }

    public IReadOnlyList<int> GetEntitiesByType(string entityType)

    {
        return _entitiesByType.TryGetValue(entityType, out var entities) ? entities : [];
    }

    public StepValue GetArgument(int entityIndex, int argumentIndex) => Table.GetArgumentValue(entityIndex, argumentIndex);

    public static string ReadString(StepValue value)
    {
        return value.Kind switch
        {
            StepValueKind.String => value.Text,
            StepValueKind.Enum => value.Text,
            StepValueKind.Raw => value.Text,
            StepValueKind.Number => value.Number.ToString(CultureInfo.InvariantCulture),
            _ => string.Empty
        };
    }

    public string ReadStringOrDefault(int entityIndex, int argumentIndex, string defaultValue = "")
    {
        var value = GetArgument(entityIndex, argumentIndex);
        var text = ReadString(value);
        return string.IsNullOrEmpty(text) ? defaultValue : text;
    }

    public string ReadGlobalId(int entityIndex)
    {
        var cached = _globalIdCache[entityIndex];
        if (!string.IsNullOrEmpty(cached))
        {
            return cached;
        }

        var globalId = ReadStringOrDefault(entityIndex, 0, $"#{Table.GetEntityId(entityIndex)}");
        _globalIdCache[entityIndex] = globalId;
        return globalId;
    }

    public void ForEachReferenceInArgument(int entityIndex, int argumentIndex, Action<int> action)
    {
        var value = GetArgument(entityIndex, argumentIndex);

        if (value.Kind == StepValueKind.Reference)
        {
            if (Table.TryGetEntityIndex(value.ReferenceId, out var targetIndex))
            {
                action(targetIndex);
            }

            return;
        }

        if (value.Kind != StepValueKind.List)
        {
            return;
        }

        Table.ForEachListItem(value, item =>
        {
            if (item.Kind != StepValueKind.Reference)
            {
                return;
            }

            if (Table.TryGetEntityIndex(item.ReferenceId, out var targetIndex))
            {
                action(targetIndex);
            }
        });
    }

    public IReadOnlyList<int> GetMaterialLayerSetsContainingLayer(int layerEntityIndex)
    {
        return _materialLayerToLayerSets.TryGetValue(layerEntityIndex, out var layerSets) ? layerSets : [];
    }

    private Dictionary<int, List<int>> BuildLayerToLayerSetMap()
    {
        var map = new Dictionary<int, List<int>>();

        var layerSets = GetEntitiesByType("IFCMATERIALLAYERSET");
        foreach (var layerSetIndex in layerSets)
        {
            ForEachReferenceInArgument(layerSetIndex, 0, layerIndex =>
            {
                if (!map.TryGetValue(layerIndex, out var layerSetList))
                {
                    layerSetList = [];
                    map[layerIndex] = layerSetList;
                }

                layerSetList.Add(layerSetIndex);
            });
        }

        return map;
    }
}

internal sealed class FastIfcStepParser
{
    public static FastIfcDataModel Parse(FileInfo ifcSourceFile, MemoryScalingOptions memoryScalingOptions)
    {
        return memoryScalingOptions.Mode switch
        {
            IntermediateStoreMode.MemoryMapped => ParseWithMemoryMappedIntermediateStore(ifcSourceFile, memoryScalingOptions),
            _ => ParseInMemory(ifcSourceFile)
        };
    }

    private static FastIfcDataModel ParseInMemory(FileInfo ifcSourceFile)
    {
        using var stream = ifcSourceFile.OpenRead();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: MemoryScalingOptions.Default.SegmentSizeBytes);

        return ParseWithStreamReader(
            reader,
            new MemoryScalingOptions(IntermediateStoreMode.Disabled, MemoryScalingOptions.Default.SegmentSizeBytes, MemoryScalingOptions.Default.SpillDirectory),
            ifcSourceFile.Name);
    }


    private static FastIfcDataModel ParseWithMemoryMappedIntermediateStore(FileInfo ifcSourceFile, MemoryScalingOptions options)
    {
        Directory.CreateDirectory(options.SpillDirectory.FullName);

        using var mmf = MemoryMappedFile.CreateFromFile(ifcSourceFile.FullName, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        using var stream = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: options.SegmentSizeBytes);

        return ParseWithStreamReader(reader, options, ifcSourceFile.Name);
    }

    private static FastIfcDataModel ParseWithStreamReader(StreamReader reader, MemoryScalingOptions options, string sourceFileName)
    {
        var builder = new Builder();
        var segmentBuffer = new char[Math.Max(1024, options.SegmentSizeBytes / 2)];
        var collector = new EntityCollector(options, sourceFileName);
        var isInsideData = false;

        while (true)
        {
            var read = reader.Read(segmentBuffer, 0, segmentBuffer.Length);
            if (read <= 0)
            {
                break;
            }

            for (var i = 0; i < read; i++)
            {
                var ch = segmentBuffer[i];
                if (!isInsideData)
                {
                    if (collector.TryEnterDataSection(ch))
                    {
                        isInsideData = true;
                    }

                    continue;
                }

                if (collector.TryAppendDataChar(ch, out var completedEntityText, out var isEndSectionReached) && completedEntityText.Length > 0)
                {
                    var entityScanner = new Scanner(completedEntityText, 0, completedEntityText.Length);
                    if (entityScanner.TryParseNextRelevantEntity(builder.ShouldIncludeEntityType, out var parsedEntity))
                    {
                        builder.AddEntity(parsedEntity);
                    }
                }


                if (isEndSectionReached)
                {
                    return new FastIfcDataModel(builder.Build());
                }
            }
        }

        throw new FastParseHeaderException("IFC DATA ENDSEC is not found.");
    }

    private sealed class EntityCollector(MemoryScalingOptions options, string sourceName)
    {
        private const string DataSectionMarker = "DATA;";
        private const string EndSectionMarker = "ENDSEC;";

        private readonly int _spillThresholdChars = options.Mode == IntermediateStoreMode.Disabled
            ? int.MaxValue
            : Math.Max(1024, options.SegmentSizeBytes / 2);
        private readonly DirectoryInfo _spillDirectory = options.SpillDirectory;
        private readonly StringBuilder _entityBuffer = new(256);
        private SpillAccumulator? _spillAccumulator;
        private bool _inString;
        private bool _hasSeenEntityStart;
        private bool _hasPendingEntityStart;
        private bool _hasPendingStringQuote;
        private int _nestingDepth;
        private int _dataMarkerMatchLength;
        private int _endSectionMatchLength;


        public bool TryEnterDataSection(char ch)
        {
            _dataMarkerMatchLength = AdvanceCaseInsensitiveMatch(_dataMarkerMatchLength, ch, DataSectionMarker);
            if (_dataMarkerMatchLength != DataSectionMarker.Length)
            {
                return false;
            }

            _dataMarkerMatchLength = 0;
            return true;
        }

        public bool TryAppendDataChar(char ch, out string completedEntityText, out bool isEndSectionReached)
        {
            completedEntityText = string.Empty;
            isEndSectionReached = false;

            if (_hasSeenEntityStart)
            {
                AppendEntityChar(ch);
                UpdateEntityState(ch);

                if (ch == ';' && !_inString && _nestingDepth == 0)
                {
                    completedEntityText = FinalizeEntity();
                    return true;
                }

                return false;
            }

            _endSectionMatchLength = AdvanceCaseInsensitiveMatch(_endSectionMatchLength, ch, EndSectionMarker);
            if (_endSectionMatchLength == EndSectionMarker.Length)
            {
                isEndSectionReached = true;
                _endSectionMatchLength = 0;
                return false;
            }

            if (!_hasPendingEntityStart)
            {
                if (ch == '#')
                {
                    _hasPendingEntityStart = true;
                }

                return false;
            }

            if (!char.IsAsciiDigit(ch))
            {
                _hasPendingEntityStart = ch == '#';
                return false;
            }

            _hasPendingEntityStart = false;
            BeginEntity('#', ch);
            return false;
        }

        private void BeginEntity(char marker, char firstIdChar)
        {
            _hasSeenEntityStart = true;
            _inString = false;
            _hasPendingStringQuote = false;
            _nestingDepth = 0;
            _entityBuffer.Clear();
            _spillAccumulator?.Dispose();
            _spillAccumulator = null;

            AppendEntityChar(marker);
            AppendEntityChar(firstIdChar);
        }

        private void UpdateEntityState(char ch)
        {
            if (_inString)
            {
                if (_hasPendingStringQuote)
                {
                    if (ch == '\'')
                    {
                        _hasPendingStringQuote = false;
                        return;
                    }

                    _inString = false;
                    _hasPendingStringQuote = false;
                }
                else
                {
                    if (ch == '\'')
                    {
                        _hasPendingStringQuote = true;
                    }

                    return;
                }
            }

            switch (ch)
            {
                case '\'':
                    _inString = true;
                    _hasPendingStringQuote = false;
                    break;
                case '(':
                    _nestingDepth++;
                    break;
                case ')':
                    if (_nestingDepth > 0)
                    {
                        _nestingDepth--;
                    }
                    break;
            }
        }

        private void AppendEntityChar(char ch)
        {
            if (_spillAccumulator is not null)
            {
                _spillAccumulator.Append(ch);
                return;
            }

            _entityBuffer.Append(ch);
            if (_entityBuffer.Length < _spillThresholdChars)
            {
                return;
            }

            _spillAccumulator = new SpillAccumulator(_spillDirectory, sourceName, _entityBuffer.ToString());
            _entityBuffer.Clear();
        }

        private string FinalizeEntity()
        {
            var result = _spillAccumulator is null
                ? _entityBuffer.ToString()
                : _spillAccumulator.ReadAllAndReset();

            _spillAccumulator?.Dispose();
            _spillAccumulator = null;
            _entityBuffer.Clear();
            _hasSeenEntityStart = false;
            _inString = false;
            _hasPendingStringQuote = false;
            _nestingDepth = 0;

            return result;
        }

        private static int AdvanceCaseInsensitiveMatch(int currentMatchLength, char ch, string marker)
        {
            var normalized = char.ToUpperInvariant(ch);
            var expected = marker[currentMatchLength];
            if (normalized == expected)
            {
                return currentMatchLength + 1;
            }

            return normalized == marker[0] ? 1 : 0;
        }
    }

    private sealed class SpillAccumulator : IDisposable
    {
        private readonly string _spillFilePath;
        private readonly StreamWriter _writer;

        public SpillAccumulator(DirectoryInfo spillDirectory, string sourceName, string initialContent)
        {
            _spillFilePath = Path.Combine(spillDirectory.FullName, $"{Path.GetFileNameWithoutExtension(sourceName)}-{Guid.NewGuid():N}.spill");
            _writer = new StreamWriter(new FileStream(_spillFilePath, FileMode.Create, FileAccess.Write, FileShare.None));
            _writer.Write(initialContent);
        }

        public void Append(char ch)
        {
            _writer.Write(ch);
        }

        public string ReadAllAndReset()
        {
            _writer.Dispose();
            var text = File.ReadAllText(_spillFilePath);
            File.Delete(_spillFilePath);
            return text;
        }

        public void Dispose()
        {
            _writer.Dispose();
            if (File.Exists(_spillFilePath))
            {
                File.Delete(_spillFilePath);
            }
        }
    }

    private sealed class Builder
    {
        private readonly List<int> _entityIds = [];
        private readonly List<string> _entityTypes = [];
        private readonly List<int> _argOffsets = [];
        private readonly List<int> _argCounts = [];
        private readonly List<int> _argValueIndices = [];
        private readonly List<StepValue> _values = [];
        private readonly List<int> _listItems = [];
        private readonly Dictionary<int, int> _entityIndexById = new();
        private readonly Dictionary<string, string> _typeNameCache = new(StringComparer.Ordinal);
        private readonly HashSet<string> _includedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "IFCMATERIAL",
            "IFCMATERIALLIST",
            "IFCMATERIALLAYERSET",
            "IFCMATERIALLAYER",
            "IFCMATERIALCONSTITUENT",
            "IFCMATERIALCONSTITUENTSET",
            "IFCMATERIALLAYERSETUSAGE",
            "IFCPIPESEGMENTTYPE",
            "IFCSPACETYPE",
            "IFCCOLUMNTYPE",
            "IFCWALLTYPE",
            "IFCSLABTYPE",
            "IFCCOVERINGTYPE",
            "IFCSTAIRFLIGHTTYPE",
            "IFCPLATETYPE",
            "IFCMEMBERTYPE",
            "IFCCURTAINWALLTYPE",
            "IFCDISTRIBUTIONELEMENTTYPE",
            "IFCBUILDINGELEMENTPROXYTYPE",
            "IFCFURNITURETYPE",
            "IFCPROPERTYSET",
            "IFCDOORLININGPROPERTIES",
            "IFCDOORPANELPROPERTIES",
            "IFCWINDOWLININGPROPERTIES",
            "IFCPROPERTYSINGLEVALUE"
        };

        public bool ShouldIncludeEntityType(string entityType)
        {
            return _includedTypes.Contains(entityType);
        }

        public void AddEntity(ParsedEntity entity)

        {
            var entityIndex = _entityIds.Count;
            _entityIds.Add(entity.Id);
            _entityTypes.Add(NormalizeTypeName(entity.Type));

            _argOffsets.Add(_argValueIndices.Count);
            _argCounts.Add(entity.Arguments.Count);
            _entityIndexById[entity.Id] = entityIndex;

            foreach (var argument in entity.Arguments)
            {
                var valueIndex = AddValue(argument);
                _argValueIndices.Add(valueIndex);
            }
        }

        public StepEntityTable Build()
        {
            return new StepEntityTable(
                _entityIds.ToArray(),
                _entityTypes.ToArray(),
                _argOffsets.ToArray(),
                _argCounts.ToArray(),
                _argValueIndices.ToArray(),
                _values.ToArray(),
                _listItems.ToArray(),
                _entityIndexById);
        }

        private int AddValue(ParsedValue value)
        {
            switch (value.Kind)
            {
                case StepValueKind.Null:
                    _values.Add(StepValue.Null());
                    return _values.Count - 1;
                case StepValueKind.Omitted:
                    _values.Add(StepValue.Omitted());
                    return _values.Count - 1;
                case StepValueKind.String:
                    _values.Add(StepValue.String(value.Text));
                    return _values.Count - 1;


                case StepValueKind.Number:
                    _values.Add(StepValue.FromNumber(value.Number));
                    return _values.Count - 1;
                case StepValueKind.Enum:
                    _values.Add(StepValue.Enum(value.Text));
                    return _values.Count - 1;


                case StepValueKind.Reference:
                    _values.Add(StepValue.Reference(value.ReferenceId));
                    return _values.Count - 1;
                case StepValueKind.Raw:
                    _values.Add(StepValue.Raw(value.Text));
                    return _values.Count - 1;


                case StepValueKind.List:
                    var listItems = value.ListItems;
                    if (listItems is null || listItems.Count == 0)
                    {
                        _values.Add(StepValue.List(_listItems.Count, 0));
                        return _values.Count - 1;
                    }

                    var listStart = _listItems.Count;
                    foreach (var listItem in listItems)
                    {
                        _listItems.Add(AddValue(listItem));
                    }

                    _values.Add(StepValue.List(listStart, listItems.Count));
                    return _values.Count - 1;

                default:
                    _values.Add(StepValue.Omitted());
                    return _values.Count - 1;
            }
        }

        private string NormalizeTypeName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (_typeNameCache.TryGetValue(value, out var cached))
            {
                return cached;
            }

            var normalized = value.ToUpperInvariant();
            if (_typeNameCache.TryGetValue(normalized, out cached))
            {
                _typeNameCache[value] = cached;
                return cached;
            }

            _typeNameCache[value] = normalized;
            if (!string.Equals(value, normalized, StringComparison.Ordinal))
            {
                _typeNameCache[normalized] = normalized;
            }

            return normalized;
        }


    }

    private readonly record struct ParsedEntity(int Id, string Type, List<ParsedValue> Arguments);

    private readonly record struct ParsedValue(StepValueKind Kind, string Text, double Number, int ReferenceId, List<ParsedValue>? ListItems)
    {
        public static ParsedValue Null() => new(StepValueKind.Null, string.Empty, 0, 0, null);
        public static ParsedValue Omitted() => new(StepValueKind.Omitted, string.Empty, 0, 0, null);
        public static ParsedValue String(string value) => new(StepValueKind.String, value, 0, 0, null);
        public static ParsedValue FromNumber(double value) => new(StepValueKind.Number, string.Empty, value, 0, null);
        public static ParsedValue Enum(string value) => new(StepValueKind.Enum, value, 0, 0, null);
        public static ParsedValue Reference(int value) => new(StepValueKind.Reference, string.Empty, 0, value, null);
        public static ParsedValue List(List<ParsedValue> value) => new(StepValueKind.List, string.Empty, 0, 0, value);
        public static ParsedValue Raw(string value) => new(StepValueKind.Raw, value, 0, 0, null);
    }

    private sealed class Scanner(string content, int start, int end)
    {
        private int _position = start;

        public bool TryParseNextRelevantEntity(Func<string, bool> shouldIncludeEntityType, out ParsedEntity entity)
        {
            while (MoveToNextEntity())
            {
                Expect('#');
                var entityId = ParseInt();
                SkipWhitespaces();
                Expect('=');
                SkipWhitespaces();
                var entityType = ParseIdentifier();

                SkipWhitespaces();
                Expect('(');

                if (!shouldIncludeEntityType(entityType))
                {
                    SkipEntityBody();
                    continue;
                }

                var arguments = ParseArguments();
                Expect(')');
                SkipWhitespaces();
                Expect(';');
                entity = new ParsedEntity(entityId, entityType, arguments);
                return true;
            }

            entity = default;
            return false;
        }

        private bool MoveToNextEntity()
        {
            while (_position < end)
            {
                if (content[_position] == '#')
                {
                    return true;
                }

                _position++;
            }

            return false;
        }

        private void SkipEntityBody()
        {
            var nestingDepth = 1;
            var inString = false;

            while (_position < end)
            {
                var ch = content[_position++];

                if (inString)
                {
                    if (ch != '\'')
                    {
                        continue;
                    }

                    if (_position < end && content[_position] == '\'')
                    {
                        _position++;
                        continue;
                    }

                    inString = false;
                    continue;
                }

                switch (ch)
                {
                    case '\'':
                        inString = true;
                        break;
                    case '(':
                        nestingDepth++;
                        break;
                    case ')':
                        nestingDepth--;
                        if (nestingDepth > 0)
                        {
                            break;
                        }

                        SkipWhitespaces();
                        if (_position < end && content[_position] == ';')
                        {
                            _position++;
                        }

                        return;
                }
            }

            throw new FastParseHeaderException("Invalid IFC content: unterminated entity body.");
        }

        private List<ParsedValue> ParseArguments()
        {
            var arguments = new List<ParsedValue>();

            while (_position < end)
            {
                SkipWhitespaces();
                if (Peek(')'))
                {
                    break;
                }

                var value = ParseValue();
                arguments.Add(value);

                SkipWhitespaces();
                if (Peek(','))
                {
                    _position++;
                    continue;
                }

                break;
            }

            return arguments;
        }

        private ParsedValue ParseValue()
        {
            SkipWhitespaces();
            if (_position >= end)
            {
                return ParsedValue.Omitted();
            }

            var ch = content[_position];
            if (ch == '$')
            {
                _position++;
                return ParsedValue.Null();
            }

            if (ch == '*')
            {
                _position++;
                return ParsedValue.Omitted();
            }

            if (ch == '#')
            {
                _position++;
                var refId = ParseInt();
                return ParsedValue.Reference(refId);
            }

            if (ch == '\'')
            {
                return ParsedValue.String(ParseStringLiteral());
            }

            if (ch == '.')
            {
                return ParsedValue.Enum(ParseEnumLiteral());
            }

            if (ch == '(')
            {
                _position++;
                var list = new List<ParsedValue>();
                while (_position < end)
                {
                    SkipWhitespaces();
                    if (Peek(')'))
                    {
                        _position++;
                        break;
                    }

                    list.Add(ParseValue());
                    SkipWhitespaces();
                    if (Peek(','))
                    {
                        _position++;
                    }
                }

                return ParsedValue.List(list);
            }

            if (char.IsAsciiDigit(ch) || ch == '-' || ch == '+')
            {
                return ParsedValue.FromNumber(ParseNumber());
            }

            if (char.IsAsciiLetter(ch))
            {
                var identifier = ParseIdentifier();
                SkipWhitespaces();
                if (Peek('('))
                {
                    _position++;
                    var wrapped = ParseWrappedValues();
                    return wrapped.Count == 1 ? wrapped[0] : ParsedValue.List(wrapped);
                }

                return ParsedValue.Raw(identifier);
            }

            return ParsedValue.Raw(ParseRawToken());
        }

        private List<ParsedValue> ParseWrappedValues()
        {
            var values = new List<ParsedValue>();
            while (_position < end)
            {
                SkipWhitespaces();
                if (Peek(')'))
                {
                    _position++;
                    break;
                }

                values.Add(ParseValue());
                SkipWhitespaces();
                if (Peek(','))
                {
                    _position++;
                }
            }

            return values;
        }

        private int ParseInt()
        {
            SkipWhitespaces();

            var start = _position;
            var value = 0;
            while (_position < end && char.IsAsciiDigit(content[_position]))
            {
                value = (value * 10) + (content[_position] - '0');
                _position++;
            }

            if (_position == start)
            {
                throw new FastParseHeaderException($"Invalid IFC integer near position {_position}.");
            }

            return value;
        }

        private double ParseNumber()
        {
            SkipWhitespaces();
            var start = _position;
            while (_position < end)
            {
                var ch = content[_position];
                if (char.IsAsciiDigit(ch) || ch == '-' || ch == '+' || ch == '.' || ch == 'E' || ch == 'e')
                {
                    _position++;
                    continue;
                }

                break;
            }

            var length = _position - start;
            if (length <= 0)
            {
                return 0;
            }

            var span = content.AsSpan(start, length);
            return double.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0;
        }

        private string ParseIdentifier()
        {
            SkipWhitespaces();
            var start = _position;
            while (_position < end)
            {
                var ch = content[_position];
                if (char.IsAsciiLetterOrDigit(ch) || ch == '_')
                {
                    _position++;
                    continue;
                }

                break;
            }

            var length = _position - start;
            return length <= 0 ? string.Empty : new string(content.AsSpan(start, length));
        }

        private string ParseStringLiteral()
        {
            Expect('\'');

            var buffer = ArrayPool<char>.Shared.Rent(128);
            var length = 0;

            try
            {
                while (_position < end)
                {
                    var ch = content[_position++];
                    if (ch == '\'')
                    {
                        if (_position < end && content[_position] == '\'')
                        {
                            EnsureBufferCapacity(ref buffer, length + 1, length);
                            buffer[length++] = '\'';

                            _position++;
                            continue;
                        }

                        break;
                    }

                    EnsureBufferCapacity(ref buffer, length + 1, length);
                    buffer[length++] = ch;

                }

                return DecodeIfcUnicodeEscapes(buffer.AsSpan(0, length));
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer);
            }
        }


        private static string DecodeIfcUnicodeEscapes(ReadOnlySpan<char> value)
        {
            if (value.IndexOf("\\X", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return new string(value);
            }

            var decoded = new StringBuilder(value.Length);
            var i = 0;

            while (i < value.Length)
            {
                if (TryDecodeUnicodeBlock(value, ref i, decoded)) continue;
                decoded.Append(value[i]);
                i++;
            }

            return decoded.ToString();
        }

        private static bool TryDecodeUnicodeBlock(ReadOnlySpan<char> value, ref int i, StringBuilder output)
        {
            if (i + 3 >= value.Length || value[i] != '\\')
            {
                return false;
            }

            var x = value[i + 1];
            if ((x != 'X' && x != 'x') || value[i + 3] != '\\')
            {
                return false;
            }

            var width = value[i + 2] switch
            {
                '2' => 4,
                '4' => 8,
                _ => 0
            };

            if (width == 0)
            {
                return false;
            }

            var payloadStart = i + 4;
            var payloadEnd = FindUnicodeBlockEnd(value, payloadStart);
            if (payloadEnd < 0)
            {
                return false;
            }

            var payload = value.Slice(payloadStart, payloadEnd - payloadStart);
            if (!TryDecodeHexPayload(payload, width, output))
            {
                output.Append(value.Slice(i, payloadEnd + 4 - i));
            }

            i = payloadEnd + 4;
            return true;
        }

        private static int FindUnicodeBlockEnd(ReadOnlySpan<char> value, int start)
        {
            for (var j = start; j + 3 < value.Length; j++)
            {
                if (value[j] != '\\')
                {
                    continue;
                }

                var x = value[j + 1];
                if (x is 'X' or 'x' && value[j + 2] == '0' && value[j + 3] == '\\')
                {
                    return j;
                }
            }

            return -1;
        }

        private static bool TryDecodeHexPayload(ReadOnlySpan<char> payload, int width, StringBuilder output)
        {
            if (payload.Length == 0 || payload.Length % width != 0)
            {
                return false;
            }

            for (var index = 0; index < payload.Length; index += width)
            {
                var chunk = payload.Slice(index, width);
                if (!TryParseHex(chunk, out var codePoint))
                {
                    return false;
                }

                if (!TryAppendCodePoint(output, codePoint))
                {
                    return false;
                }

            }

            return true;
        }

        private static bool TryParseHex(ReadOnlySpan<char> chunk, out int value)
        {
            value = 0;
            for (var i = 0; i < chunk.Length; i++)
            {
                var ch = chunk[i];
                var digit = ch switch
                {
                    >= '0' and <= '9' => ch - '0',
                    >= 'A' and <= 'F' => ch - 'A' + 10,
                    >= 'a' and <= 'f' => ch - 'a' + 10,
                    _ => -1
                };

                if (digit < 0)
                {
                    return false;
                }

                value = (value << 4) + digit;
            }

            return true;
        }

        private static bool TryAppendCodePoint(StringBuilder output, int codePoint)
        {
            if (codePoint < 0 || codePoint > 0x10FFFF || (codePoint >= 0xD800 && codePoint <= 0xDFFF))
            {
                return false;
            }

            if (codePoint <= 0xFFFF)
            {
                output.Append((char)codePoint);
                return true;
            }

            codePoint -= 0x10000;
            output.Append((char)((codePoint >> 10) + 0xD800));
            output.Append((char)((codePoint & 0x3FF) + 0xDC00));
            return true;
        }

        private string ParseEnumLiteral()
        {
            Expect('.');
            var start = _position;
            while (_position < end && content[_position] != '.')
            {
                _position++;
            }

            var value = content[start.._position];
            Expect('.');
            return value;
        }

        private string ParseRawToken()
        {
            var start = _position;
            while (_position < end)
            {
                var ch = content[_position];
                if (ch is ',' or ')' or ';')
                {
                    break;
                }

                _position++;
            }

            var span = content.AsSpan(start, _position - start).Trim();
            return span.IsEmpty ? string.Empty : new string(span);
        }

        private static void EnsureBufferCapacity(ref char[] buffer, int requiredLength, int currentLength)
        {
            if (requiredLength <= buffer.Length)
            {
                return;
            }

            var expanded = ArrayPool<char>.Shared.Rent(buffer.Length * 2);
            buffer.AsSpan(0, currentLength).CopyTo(expanded);
            ArrayPool<char>.Shared.Return(buffer);
            buffer = expanded;
        }


        private bool Peek(char ch)
        {
            return _position < end && content[_position] == ch;
        }

        private void Expect(char ch)
        {
            SkipWhitespaces();
            if (_position >= end || content[_position] != ch)
            {
                throw new FastParseHeaderException($"Invalid IFC content near position {_position}. Expected '{ch}'.");
            }

            _position++;
        }

        private void SkipWhitespaces()
        {
            while (_position < end && char.IsWhiteSpace(content[_position]))
            {
                _position++;
            }
        }
    }
}
