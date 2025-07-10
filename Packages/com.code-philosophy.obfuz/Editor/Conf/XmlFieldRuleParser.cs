﻿using dnlib.DotNet;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEditor.VersionControl;
using UnityEngine;

namespace Obfuz.Conf
{



    public class XmlFieldRuleParser<R> where R : class, new()
    {
        private readonly HashSet<string> _toObfuscatedAssemblyNames;
        private readonly Func<string, XmlElement, R> _ruleParser;
        private readonly Action<string, XmlElement> _unknownNodeTypeHandler;
        private readonly Dictionary<string, AssemblySpec> _assemblySpecs = new Dictionary<string, AssemblySpec>();


        private class FieldSpec
        {
            public string Name { get; set; }
            public NameMatcher NameMatcher { get; set; }

            public R Rule { get; set; }
        }

        private class TypeSpec
        {
            public string Name { get; set; }

            public NameMatcher NameMatcher { get; set; }

            public List<FieldSpec> Fields { get; set; }
        }

        private class AssemblySpec
        {
            public string Name { get; set; }

            public List<TypeSpec> Types { get; set; }
        }

        public XmlFieldRuleParser(IEnumerable<string> toObfuscatedAssemblyNames, Func<string, XmlElement, R> ruleParser, Action<string, XmlElement> unknownNodeTypeHandler)
        {
            _toObfuscatedAssemblyNames = new HashSet<string>(toObfuscatedAssemblyNames);
            _ruleParser = ruleParser;
            _unknownNodeTypeHandler = unknownNodeTypeHandler;
        }

        public void LoadConfigs(IEnumerable<string> configFiles)
        {
            foreach (var configFile in configFiles)
            {
                LoadConfig(configFile);
            }
        }

        public void LoadConfig(string configFile)
        {
            if (string.IsNullOrEmpty(configFile))
            {
                throw new Exception($"Invalid xml file {configFile}, file name is empty");
            }
            var doc = new XmlDocument();
            doc.Load(configFile);
            var root = doc.DocumentElement;
            if (root.Name != "obfuz")
            {
                throw new Exception($"Invalid xml file {configFile}, root name should be 'obfuz'");
            }
            foreach (XmlNode node in root.ChildNodes)
            {
                if (!(node is XmlElement ele))
                {
                    continue;
                }
                switch (ele.Name)
                {
                    case "assembly":
                    {
                        AssemblySpec assSpec = ParseAssembly(configFile, ele);
                        _assemblySpecs.Add(assSpec.Name, assSpec);
                        break;
                    }
                    default:
                    {
                        if (_unknownNodeTypeHandler == null)
                        {
                            throw new Exception($"Invalid xml file {configFile}, unknown node {ele.Name}");
                        }
                        _unknownNodeTypeHandler(configFile, ele);
                        break;
                    }
                }
            }
        }

        private AssemblySpec ParseAssembly(string configFile, XmlElement ele)
        {
            var assemblySpec = new AssemblySpec();
            string name = ele.GetAttribute("name");
            if (!_toObfuscatedAssemblyNames.Contains(name))
            {
                throw new Exception($"Invalid xml file {configFile}, assembly name {name} isn't in toObfuscatedAssemblyNames");
            }
            if (_assemblySpecs.ContainsKey(name))
            {
                throw new Exception($"Invalid xml file {configFile}, assembly name {name} is duplicated");
            }
            assemblySpec.Name = name;

            var types = new List<TypeSpec>();
            assemblySpec.Types = types;
            foreach (XmlNode node in ele.ChildNodes)
            {
                if (!(node is XmlElement childEle))
                {
                    continue;
                }
                switch (childEle.Name)
                {
                    case "type":
                    {
                        types.Add(ParseType(configFile, childEle));
                        break;
                    }
                    default:
                    {
                        throw new Exception($"Invalid xml file, unknown node {childEle.Name}");
                    }
                }
            }
            return assemblySpec;
        }

        private TypeSpec ParseType(string configFile, XmlElement element)
        {
            var typeSpec = new TypeSpec();

            string name = element.GetAttribute("name");
            typeSpec.Name = name;
            typeSpec.NameMatcher = new NameMatcher(name);

            var fields = new List<FieldSpec>();
            typeSpec.Fields = fields;
            foreach (XmlNode node in element.ChildNodes)
            {
                if (!(node is XmlElement ele))
                {
                    continue;
                }
                switch (ele.Name)
                {
                    case "field":
                    {
                        fields.Add(ParseField(configFile, ele));
                        break;
                    }
                    default:
                    {
                        throw new Exception($"Invalid xml file, unknown node {ele.Name}");
                    }
                }
            }
            return typeSpec;
        }

        private FieldSpec ParseField(string configFile, XmlElement element)
        {
            var fieldSpec = new FieldSpec();
            string name = element.GetAttribute("name");
            fieldSpec.Name = name;
            fieldSpec.NameMatcher = new NameMatcher(name);
            fieldSpec.Rule = _ruleParser(configFile, element);
            return fieldSpec;
        }

        public R GetFieldRule(FieldDef field)
        {
            var assemblyName = field.DeclaringType.Module.Assembly.Name;
            if (!_assemblySpecs.TryGetValue(assemblyName, out var assSpec))
            {
                return null;
            }
            string declaringTypeName = field.DeclaringType.FullName;
            foreach (var typeSpec in assSpec.Types)
            {
                if (typeSpec.NameMatcher.IsMatch(declaringTypeName))
                {
                    foreach (var fieldSpec in typeSpec.Fields)
                    {
                        if (fieldSpec.NameMatcher.IsMatch(field.Name))
                        {
                            return fieldSpec.Rule;
                        }
                    }
                }
            }
            return null;
        }
    }
}
