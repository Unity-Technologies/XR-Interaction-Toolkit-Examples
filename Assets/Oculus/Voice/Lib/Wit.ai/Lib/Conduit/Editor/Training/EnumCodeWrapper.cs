/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Meta.WitAi;
using Microsoft.CSharp;

namespace Meta.Conduit.Editor
{
    /// <summary>
    /// Wraps around an Enum in code to allow querying and modifying its source code in a single source file.
    /// </summary>
    internal class EnumCodeWrapper
    {
        public const string DEFAULT_PATH = @"Assets\";
        
        private readonly string _sourceFilePath;
        private readonly IFileIo _fileIo;
        private readonly CodeCompileUnit _compileUnit;
        private readonly CodeTypeDeclaration _typeDeclaration;
        private readonly List<string> _enumValues = new List<string>();
        private readonly CodeDomProvider _provider = new CSharpCodeProvider();
        private readonly Dictionary<string, CodeNamespace> _namespaces = new Dictionary<string, CodeNamespace>();
        private readonly Action<CodeNamespace> _namespaceSetup;
        private readonly Action<CodeMemberField> _memberSetup;
        private readonly string _conduitAttributeName;
        private readonly CodeNamespace _namespace;

        // Setup with existing enum
        public EnumCodeWrapper(IFileIo fileIo, Type enumType, string entityName, string sourceCodeFile) : this(fileIo, enumType.Name, entityName, null, enumType.Namespace, sourceCodeFile)
        {
            if (!enumType.IsEnum)
            {
                throw new ArgumentException("Type must be an enumeration.", nameof(enumType));
            }

            var enumValues = new List<WitKeyword>();
            foreach (var enumValueName in enumType.GetEnumNames())
            {
                var aliases = GetAliases(enumType, enumValueName);
                enumValues.Add(new WitKeyword(aliases[0], aliases.GetRange(1, aliases.Count - 1)));
            }
            
            AddValues(enumValues);
        }

        // Setup
        public EnumCodeWrapper(IFileIo fileIo, string enumName, string entityName, IList<WitKeyword> enumValues, string enumNamespace = null, string sourceCodeFile = null)
        {
            if (string.IsNullOrEmpty(enumName))
            {
                throw new ArgumentException(nameof(enumName));
            }
            if (string.IsNullOrEmpty(entityName))
            {
                throw new ArgumentException(nameof(entityName));
            }
            
            _conduitAttributeName = GetShortAttributeName(nameof(ConduitValueAttribute));
            
            // Initial setup
            _compileUnit = new CodeCompileUnit();
            _sourceFilePath = string.IsNullOrEmpty(sourceCodeFile) ? GetEnumFilePath(enumName, enumNamespace) : sourceCodeFile;
            _fileIo = fileIo;

            // Setup namespace
            if (string.IsNullOrEmpty(enumNamespace))
            {
                _namespace = new CodeNamespace();
            }
            else
            {
                _namespace = new CodeNamespace(enumNamespace);
                _namespaces.Add(enumNamespace, _namespace);
            }

            _compileUnit.Namespaces.Add(_namespace);

            // Setup type declaration
            _typeDeclaration = new CodeTypeDeclaration(enumName)
            {
                IsEnum = true
            };
            _namespace.Types.Add(_typeDeclaration);

            if (!entityName.Equals(enumName))
            {
                var entityAttributeType = new CodeTypeReference(GetShortAttributeName(nameof(ConduitEntityAttribute)));
                var entityAttributeArgs = new CodeAttributeArgument[]
                {
                    new CodeAttributeArgument(new CodePrimitiveExpression(entityName))
                };
                AddEnumAttribute(new CodeAttributeDeclaration(entityAttributeType, entityAttributeArgs));
            }

            // Add all enum values
            AddValues(enumValues);
        }
        
        /// <summary>
        /// Adds the supplied values to the enum construct. Values that already exist are ignored.
        /// </summary>
        /// <param name="values">The values to add.</param>
        public void AddValues(IList<WitKeyword> values)
        {
            if (values == null)
            {
                return;
            }

            foreach (var value in values)
            {
                AddValue(value);
            }
        }
        
        public void AddValue(WitKeyword keyword)
        {
            var pendingSynonyms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            pendingSynonyms.Add(keyword.keyword);
            List<CodeAttributeArgument> arguments = new List<CodeAttributeArgument>();

            if (keyword.synonyms != null)
            {
                foreach (var synonym in keyword.synonyms)
                {
                    if (!pendingSynonyms.Contains(synonym))
                        //if (synonym.ToLower() != keyword.keyword.ToLower())
                    {
                        pendingSynonyms.Add(synonym);
                        arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(synonym)));
                    }
                }
            }
            
            CodeAttributeDeclaration codeAttribute = null;
            if (arguments.Count > 0)
            {
                var entityKeywordAttributeType =
                    new CodeTypeReference(_conduitAttributeName);
                codeAttribute = new CodeAttributeDeclaration(entityKeywordAttributeType, arguments.ToArray());
            }
            
            AddValue(keyword.keyword, codeAttribute);
        }

        /// <summary>
        /// Returns a list of all aliases for the keyword starting with the original keyword.
        /// </summary>
        /// <param name="enumType">The enum type.</param>
        /// <param name="enumValueName">The name of the enum value we are getting aliases for.</param>
        /// <returns>The list of aliases starting with the keyword itself.</returns>
        private List<string> GetAliases(Type enumType, string enumValueName)
        {
            var enumValueInfo = enumType.GetMember(enumValueName);
            var enumValueMemberInfo = enumValueInfo.FirstOrDefault(info => info.DeclaringType == enumType);
            if (enumValueMemberInfo == null)
            {
                return new List<string>() { enumValueName };
            }

            var allAliases = new List<string>() { enumValueName }; 
            
            var attribute = enumValueMemberInfo.GetCustomAttributes(typeof(ConduitValueAttribute), false).FirstOrDefault() as ConduitValueAttribute;
            if (attribute == null)
            {
                return allAliases;
            }

            allAliases.AddRange(attribute.Aliases.Where(alias => alias != enumValueName));

            return allAliases;
        }
        
        private void ImportConduitNamespaceIfNeeded()
        {
            foreach (var customAttribute in _typeDeclaration.CustomAttributes)
            {
                if (customAttribute is CodeAttributeDeclaration attribute && attribute.Name.Equals(GetShortAttributeName(nameof(ConduitEntityAttribute))))
                {
                    AddNamespaceImport(typeof(ConduitValueAttribute));
                    return;
                }
            }

            foreach (var member in _typeDeclaration.Members)
            {
                if (member is CodeMemberField field)
                {
                    if (field.CustomAttributes.Count > 0)
                    {
                        AddNamespaceImport(typeof(ConduitValueAttribute));
                        return;
                    }
                }
            }
        }

        private string GetShortAttributeName(string attributeName)
        {
            var suffix = "Attribute";
            if (attributeName.EndsWith(suffix))
            {
                attributeName = attributeName.Remove(attributeName.Length - suffix.Length);
            }

            return attributeName;
        }
        
        // Get safe enum file path
        private string GetEnumFilePath(string enumName, string enumNamespace)
        {
            return Path.Combine(DEFAULT_PATH, enumNamespace.Replace('.', '\\'), $"{enumName}.cs");
        }

        // Add namespace import
        private void AddNamespaceImport(Type forType)
        {
            if (forType == null)
            {
                return;
            }
            var attributeNamespaceName = forType.Namespace;
            var importNameSpace = new CodeNamespaceImport(attributeNamespaceName);
            
            if (_namespace == null)
            {
                VLog.E("Namespace was null");
                return;
            }
            
            _namespace.Imports.Add(importNameSpace);
        }

        // Add enum attribute
        private void AddEnumAttribute(CodeAttributeDeclaration attribute)
        {
            if (attribute == null)
            {
                return;
            }
            _typeDeclaration.CustomAttributes.Add(attribute);
        }

        // Add a single value. Replace attribute if value already exists.
        private void AddValue(string value, CodeAttributeDeclaration attribute = null)
        {
            // Get clean value
            var cleanValue = ConduitUtilities.SanitizeString(value);

            // Get field
            var field = new CodeMemberField(_typeDeclaration.Name, cleanValue);

            // Add attribute
            if (attribute != null)
            {
                field.CustomAttributes.Add(attribute);
            }
            
            // Replace attribute if value already exists
            if (_enumValues.Contains(cleanValue))
            {
                int enumIndex = _enumValues.IndexOf(cleanValue);
                
                _typeDeclaration.Members[enumIndex] = field;
                return;
            }

            // Add to enum & members list
            _enumValues.Add(cleanValue);
            _typeDeclaration.Members.Add(field);
        }

        /// <summary>
        /// Removes the supplied values to the enum construct. Values that do not exist in the enum are ignored.
        /// </summary>
        /// <param name="values">The values to remove.</param>
        internal void RemoveValues(IList<string> values)
        {
            if (values == null)
            {
                return;
            }
            foreach (var value in values)
            {
                RemoveValue(value);
            }
        }

        /// <summary>
        /// Returns a single value
        /// </summary>
        private void RemoveValue(string value)
        {
            // Check enum names
            string cleanName = ConduitUtilities.SanitizeString(value);
            int enumIndex = _enumValues.IndexOf(cleanName);

            // Not found
            if (enumIndex == -1)
            {
                return;
            }

            // Remove enum
            _enumValues.RemoveAt(enumIndex);
            _typeDeclaration.Members.RemoveAt(enumIndex);
        }

        public void WriteToFile()
        {
            this._fileIo.WriteAllText(_sourceFilePath, this.ToSourceCode());
        }

        public string ToSourceCode()
        {
            ImportConduitNamespaceIfNeeded();
            
            // Create a TextWriter to a StreamWriter to the output file.
            var sb = new StringBuilder();
            using (var sw = new StringWriter(sb))
            {
                var tw = new IndentedTextWriter(sw, "    ");

                // Generate source code using the code provider.
                _provider.GenerateCodeFromCompileUnit(this._compileUnit, tw,
                    new CodeGeneratorOptions()
                    {
                        BracingStyle = "C",
                        BlankLinesBetweenMembers = false,
                        VerbatimOrder = true,
                    });

                // Close the output file.
                tw.Close();
            }

            return sb.ToString();
        }
    }
}
