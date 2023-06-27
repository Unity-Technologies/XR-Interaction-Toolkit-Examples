/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using Meta.WitAi.Data.Info;
using Meta.WitAi.Json;

namespace Meta.WitAi.Lib
{

    /// <summary>
    /// Parses the Wit.ai Export zip file
    /// </summary>
    public class ExportParser
    {
        private readonly ZipArchive _zip;
        private readonly List<PathValues> _sharedVariables = new List<PathValues>();
        private readonly List<PathValues> _serverVariables = new List<PathValues>();
        private readonly ArrayList _clientVariables = new ArrayList();
        private const string ComposerFolderName = "/composer/";

        private static class ModuleType
        {
            public const string Response = "response";
            public const string Decision = "decision";
            public const string Context = "context";
        }
        public ExportParser(ZipArchive zip)
        {
            _zip = zip;
        }
        /// <summary>
        /// Parses the provided zip export and extracts the context map values
        /// used within the composer graphs.
        /// </summary>
        public WitComposerInfo ImportComposerInfo()
        {
            WitComposerInfo info = new WitComposerInfo();
            info = ExtractCanvases(info, GetCanvasJsons());
            return info;
        }
        /// <summary>
        /// Finds all the Composer canvases in the zip archive
        /// </summary>
        /// <returns>new list of entries which represent canvases</returns>
        private List<ZipArchiveEntry> GetCanvasJsons()
        {
            var jsonCanvases = new List<ZipArchiveEntry>();
            foreach (var entry in _zip.Entries)
            {
                if (entry.FullName.Contains(ComposerFolderName))
                {
                    jsonCanvases.Add(entry);
                }
            }
            return jsonCanvases;
        }
        /// <summary>
        /// Parses the given zip archives and adds them to the
        /// </summary>
        /// <param name="jsonCanvases"></param>
        /// <returns></returns>
        private WitComposerInfo ExtractCanvases(WitComposerInfo info, List<ZipArchiveEntry> jsonCanvases)
        {
            info.canvases = new ComposerGraph[jsonCanvases.Count];

            for (var i = 0; i < jsonCanvases.Count; i++)
            {
                var jsonNode = ExtractCanvasJson(_zip, jsonCanvases[i].Name);
                info.canvases[i].contextMap = ParseModules(jsonNode);
                var name = Path.GetFileNameWithoutExtension(jsonCanvases[i].Name);
                name = name.Substring(0, 1).ToUpper() + name.Substring(1, name.Length - 1); //capitalize 1st letter
                info.canvases[i].canvasName = name;
            }
            return info;
        }

        /// <summary>
        /// Extracts a Wit JSON object representing the given canvas
        /// </summary>
        /// <param name="zip">zip archive from Wit.ai export</param>
        /// <param name="canvasName">one of the canvas names, defined in CanvasType</param>
        /// <returns>The entire canvas structure as nested JSON objects</returns>
        private WitResponseNode ExtractCanvasJson(ZipArchive zip, string canvasName)
        {
            var entry = zip.Entries.First((v) => v.Name.EndsWith(canvasName));
            if (entry.Name.EndsWith(canvasName))
            {
                var stream = entry.Open();
                var json = new StreamReader(stream).ReadToEnd();

                return JsonConvert.DeserializeToken(json);
            }
            VLog.W("Could not open canvas named "+ canvasName);
            return null;
        }

        /// <summary>
        ///  List-based class to process nodes more easily
        /// </summary>
        private class PathValues
        {
            public readonly string Path;
            public readonly List<string> Values = new List<string>();
            public PathValues(string path)
            {
                Path = path;
            }

            public PathValues(string path, string firstValue) :this(path)
            {
                Values.Add(firstValue);
            }

            public ComposerGraphValues ConvertToStruct()
            {
                var graphValues = new ComposerGraphValues
                {
                    path = Path,
                    values = new string[Values.Count]
                };
                Values.CopyTo(graphValues.values);
                return graphValues;
            }
        }

        /// <summary>
        /// Parses the Composer Map for context map variables
        /// </summary>
        private ContextMapPaths ParseModules(WitResponseNode json)
        {

            foreach (var module in json["modules"].Childs)
            {
                switch (module["type"].Value)
                {
                    case ModuleType.Response: // read on server, written in Unity
                        GatherMapValuesFromResponse(module);
                        break;

                    case ModuleType.Decision: // read on server, written in Unity
                        GatherMapValuesFromDecision(module);
                        break;

                    case ModuleType.Context: // readable in Unity, written on server
                        GatherMapValuesFromContext(module);
                        continue;
                }
            }
            ContextMapPaths result = ConvertToContextMapValues();
            return result;
        }

        /// <summary>
        /// Parses a Response module in the composer graph, adding any context map values to the correct list.
        /// </summary>
        /// <param name="module">the JSON representation of the module</param>
        private void GatherMapValuesFromResponse(WitResponseNode module)
        {
            var responseText = module["response_fields"]["text"].ToString();
            var matches = Regex.Match(responseText, @"{.*}");
            foreach (Group match in matches.Groups)
            {
                if (string.IsNullOrEmpty(match.Value)) continue;

                var path = match.Value.Substring(1, match.Value.Length - 2);
                if (IsPathAlreadyDiscovered(path))
                    continue;

                if (MoveToShared(path))
                    continue;
                _clientVariables.Add(path);
            }
        }


        /// <summary>
        /// Parses a Decision module in the composer graph, adding any context map values to the correct list.
        /// </summary>
        /// <param name="module"></param>
        private void GatherMapValuesFromDecision(WitResponseNode module)
        {
            var conditionNodes = module["decision_fields"]?["condition_nodes"];
            for (var i = 0; i < conditionNodes?.Count; i++)
            {
                var path = conditionNodes[i]["context_with_value_fields"]?["path"].Value;
                if (IsPathAlreadyDiscovered(path))
                    continue;
                if (MoveToShared(path))
                    continue;

                _clientVariables.Add(path);
            }
        }
        /// <summary>
        /// Moves a path/value item from server to shared list.
        /// </summary>
        /// <returns>true if the item was found and moved; false otherwise.</returns>
        private  bool MoveToShared(string path)
        {
            var found = _serverVariables.Find((item) => item.Path == path);
            if (found != null)
            {
                _serverVariables.Remove(found);
                _sharedVariables.Add(found);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Whether the given path has already been saved in one of the given lists
        /// </summary>
        private bool IsPathAlreadyDiscovered(string path)
        {
            return string.IsNullOrEmpty(path) ||
                _clientVariables.Contains(path) ||
                _sharedVariables.Exists((readable) => readable.Path == path);
        }

        /// <summary>
        /// Parses the given context node and places any context map variables in the correct list.
        /// </summary>
        private void GatherMapValuesFromContext(WitResponseNode module)
        {
            string path, value;
            var saveFields = module["context_fields"]?["save_fields"];
            var setFields = module["context_fields"]?["set_fields"];
            switch (module["context_fields"]?["context_type"])
            {
                case "save":
                    path = saveFields?["path"].ToString();
                    value = saveFields?["entity"].ToString();
                    break;
                case "set":
                    path = module["context_fields"]?["set_fields"]?["path"].ToString();
                    value = setFields?["value"].ToString();
                    break;
                default:
                    return;
            }
            path = path?.Replace("\"", "");
            value = value?.Replace("\"", "");

            //check if we have the same path and value in readwrite
            var foundWritable = _sharedVariables.Find((item) => item.Path == path);
            if (foundWritable != null && !foundWritable.Values.Contains(value))
            {
                foundWritable?.Values.Add(value);
                return;
            }

            //check if we have the same path and value in basic read
            var foundReadable = _serverVariables.Find((item) => item.Path == path);
            if (foundReadable != null && !foundReadable.Values.Contains(value))
            {
                foundReadable?.Values.Add(value);
                return;
            }

            var newReadable = new PathValues(path, value);
            if (_clientVariables.Contains(path)) //move to readWrite if would be in both.
            {
                _clientVariables.Remove(path);
                _sharedVariables.Add(newReadable);
                return;
            }
            _serverVariables.Add(newReadable);
        }

        /// <summary>
        /// Takes all three lists of variables and aggregate them into a single object
        /// </summary>
        private ContextMapPaths ConvertToContextMapValues()
        {
            var result = new ContextMapPaths();
            result.client = (string[])_clientVariables.ToArray(typeof(string));

            result.server = new ComposerGraphValues[_serverVariables.Count];
            for (var i = 0; i < _serverVariables.Count; i++) result.server[i] = _serverVariables[i].ConvertToStruct();

            result.shared = new ComposerGraphValues[_sharedVariables.Count];
            for (var i = 0; i < _sharedVariables.Count; i++) result.shared[i] = _sharedVariables[i].ConvertToStruct();

            _sharedVariables.Clear();
            _serverVariables.Clear();
            _clientVariables.Clear();

            return result;
        }
    }
}
