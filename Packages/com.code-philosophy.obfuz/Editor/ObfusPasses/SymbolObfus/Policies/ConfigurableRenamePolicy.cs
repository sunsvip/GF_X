using dnlib.DotNet;
using Obfuz.Conf;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;

namespace Obfuz.ObfusPasses.SymbolObfus.Policies
{

    public class ConfigurableRenamePolicy : ObfuscationPolicyBase
    {
        enum RuleType
        {
            Assembly = 1,
            Type = 2,
            Method = 3,
            Field = 4,
            Property = 5,
            Event = 6,
        }

        enum ModifierType
        {
            None = 0x0,
            Private = 0x1,
            Protected = 0x2,
            Public = 0x3,
        }

        class MethodRuleSpec
        {
            public NameMatcher nameMatcher;
            public ModifierType? modifierType;
            public bool? obfuscateName;
        }

        class FieldRuleSpec
        {
            public NameMatcher nameMatcher;
            public ModifierType? modifierType;
            public bool? obfuscateName;
        }

        class PropertyRuleSpec
        {
            public NameMatcher nameMatcher;
            public ModifierType? modifierType;
            public bool? obfuscateName;
            public bool applyToMethods;
        }

        class EventRuleSpec
        {
            public NameMatcher nameMatcher;
            public ModifierType? modifierType;
            public bool? obfuscateName;
            public bool applyToMethods;
        }

        class TypeRuleSpec
        {
            public NameMatcher nameMatcher;
            public ModifierType? modifierType;
            public ClassType? classType;
            public bool? obfuscateName;
            public bool applyToMembers;
            public List<FieldRuleSpec> fields;
            public List<MethodRuleSpec> methods;
            public List<PropertyRuleSpec> properties;
            public List<EventRuleSpec> events;
        }

        class AssemblyRuleSpec
        {
            public string assemblyName;
            public List<TypeRuleSpec> types;
        }

        private readonly Dictionary<string, List<AssemblyRuleSpec>> _assemblyRuleSpecs = new Dictionary<string, List<AssemblyRuleSpec>>();

        private AssemblyRuleSpec ParseAssembly(XmlElement ele)
        {
            string assemblyName = ele.GetAttribute("name");
            if (string.IsNullOrEmpty(assemblyName))
            {
                throw new Exception($"Invalid xml file, assembly name is empty");
            }
            if (!_obfuscationAssemblyNames.Contains(assemblyName))
            {
                throw new Exception($"unknown assembly name:{assemblyName}, not in ObfuzSettings.obfuscationAssemblyNames");
            }
            var rule = new AssemblyRuleSpec()
            {
                assemblyName = assemblyName,
                types = new List<TypeRuleSpec>(),
            };

            foreach (XmlNode node in ele.ChildNodes)
            {
                if (!(node is XmlElement childElement))
                {
                    continue;
                }
                if (childElement.Name != "type")
                {
                    throw new Exception($"Invalid xml file, unknown node {childElement.Name}");
                }
                TypeRuleSpec type = ParseType(childElement);
                rule.types.Add(type);
            }
            return rule;
        }

        private enum ClassType
        {
            None = 0x0,
            Class = 0x1,
            Struct = 0x2,
            Interface = 0x4,
            Enum = 0x8,
            Delegate = 0x10,
        }

        private ClassType? ParseClassType(string classType)
        {
            if (string.IsNullOrEmpty(classType))
            {
                return null;
            }
            
            ClassType type = ClassType.None;
            foreach (var s in classType.Split('|'))
            {
                switch (s)
                {
                    case "class": type |= ClassType.Class; break;
                    case "struct": type |= ClassType.Struct; break;
                    case "interface": type |= ClassType.Interface; break;
                    case "enum": type |= ClassType.Enum; break;
                    case "delegate": type |= ClassType.Delegate; break;
                    default: throw new Exception($"Invalid class type {s}");
                }
            }
            return type;
        }

        private ModifierType? ParseModifierType(string modifierType)
        {
            if (string.IsNullOrEmpty(modifierType))
            {
                return null;
            }
            ModifierType type = ModifierType.None;
            foreach (var s in modifierType.Split('|'))
            {
                switch (s)
                {
                    case "public": type |= ModifierType.Public; break;
                    case "protected": type |= ModifierType.Protected; break;
                    case "private": type |= ModifierType.Private; break;
                    default: throw new Exception($"Invalid modifier type {s}");
                }
            }
            return type;
        }

        private TypeRuleSpec ParseType(XmlElement element)
        {
            var rule = new TypeRuleSpec();

            rule.nameMatcher = new NameMatcher(element.GetAttribute("name"));
            rule.obfuscateName = ConfigUtil.ParseNullableBool(element.GetAttribute("obName"));
            rule.applyToMembers = ConfigUtil.ParseNullableBool(element.GetAttribute("applyToMembers")) ?? false;
            rule.modifierType = ParseModifierType(element.GetAttribute("modifier"));
            rule.classType = ParseClassType(element.GetAttribute("classType"));

            //rule.nestTypeRuleSpecs = new List<TypeRuleSpec>();
            rule.fields = new List<FieldRuleSpec>();
            rule.methods = new List<MethodRuleSpec>();
            rule.properties = new List<PropertyRuleSpec>();
            rule.events = new List<EventRuleSpec>();
            foreach (XmlNode node in element.ChildNodes)
            {
                if (!(node is XmlElement childElement))
                {
                    continue;
                }
                switch (childElement.Name)
                {
                    case "field":
                    {
                        var fieldRuleSpec = new FieldRuleSpec();
                        fieldRuleSpec.nameMatcher = new NameMatcher(childElement.GetAttribute("name"));
                        fieldRuleSpec.modifierType = ParseModifierType(childElement.GetAttribute("modifier"));
                        fieldRuleSpec.obfuscateName = ConfigUtil.ParseNullableBool(childElement.GetAttribute("obName"));
                        rule.fields.Add(fieldRuleSpec);
                        break;
                    }
                    case "method":
                    {
                        var methodRuleSpec = new MethodRuleSpec();
                        methodRuleSpec.nameMatcher = new NameMatcher(childElement.GetAttribute("name"));
                        methodRuleSpec.modifierType = ParseModifierType(childElement.GetAttribute("modifier"));
                        methodRuleSpec.obfuscateName = ConfigUtil.ParseNullableBool(childElement.GetAttribute("obName"));
                        rule.methods.Add(methodRuleSpec);
                        break;
                    }
                    case "property":
                    {
                        var propertyRulerSpec = new PropertyRuleSpec();
                        propertyRulerSpec.nameMatcher = new NameMatcher(childElement.GetAttribute("name"));
                        propertyRulerSpec.modifierType = ParseModifierType(childElement.GetAttribute("modifier"));
                        propertyRulerSpec.obfuscateName = ConfigUtil.ParseNullableBool(childElement.GetAttribute("obName"));
                        propertyRulerSpec.applyToMethods = ConfigUtil.ParseNullableBool(childElement.GetAttribute("applyToMethods")) ?? false;
                        rule.properties.Add(propertyRulerSpec);
                        break;
                    }
                    case "event":
                    {
                        var eventRuleSpec = new EventRuleSpec();
                        eventRuleSpec.nameMatcher = new NameMatcher(childElement.GetAttribute("name"));
                        eventRuleSpec.modifierType = ParseModifierType(childElement.GetAttribute("modifier"));
                        eventRuleSpec.obfuscateName = ConfigUtil.ParseNullableBool(childElement.GetAttribute("obName"));
                        eventRuleSpec.applyToMethods = ConfigUtil.ParseNullableBool(childElement.GetAttribute("applyToMethods")) ?? false;
                        rule.events.Add(eventRuleSpec);
                        break;
                    }
                    default: throw new Exception($"Invalid xml file, unknown node {childElement.Name} in type node");
                }
            }
            return rule;
        }

        private void LoadXmls(List<string> xmlFiles)
        {
            var rawAssemblySpecElements = new List<XmlElement>();
            foreach (string file in xmlFiles)
            {
                LoadRawXml(file, rawAssemblySpecElements);
            }
            ResolveAssemblySpecs(rawAssemblySpecElements);
        }

        private void ResolveAssemblySpecs(List<XmlElement> rawAssemblySpecElements)
        {
            foreach (XmlElement ele in rawAssemblySpecElements)
            {
                var assemblyRule = ParseAssembly(ele);
                if (!_assemblyRuleSpecs.TryGetValue(assemblyRule.assemblyName, out var existAssemblyRules))
                {
                    existAssemblyRules = new List<AssemblyRuleSpec>();
                    _assemblyRuleSpecs.Add(assemblyRule.assemblyName, existAssemblyRules);
                }
                existAssemblyRules.Add(assemblyRule);
            }
        }

        private void LoadRawXml(string xmlFile, List<XmlElement> rawAssemblyElements)
        {
            Debug.Log($"ObfuscateRule::LoadXml {xmlFile}");
            var doc = new XmlDocument();
            doc.Load(xmlFile);
            var root = doc.DocumentElement;
            if (root.Name != "obfuz")
            {
                throw new Exception($"Invalid xml file {xmlFile}, root name should be 'obfuz'");
            }
            foreach (XmlNode node in root.ChildNodes)
            {
                if (!(node is XmlElement element))
                {
                    continue;
                }
                switch (element.Name)
                {
                    case "assembly":
                    {
                        rawAssemblyElements.Add(element);
                        break;
                    }
                    default:
                    {
                        throw new Exception($"Invalid xml file {xmlFile}, unknown node {element.Name}");
                    }
                }
            }
        }

        private ModifierType ComputeModifierType(TypeAttributes visibility)
        {
            if (visibility == TypeAttributes.NotPublic || visibility == TypeAttributes.NestedPrivate)
            {
                return ModifierType.Private;
            }
            if (visibility == TypeAttributes.Public || visibility == TypeAttributes.NestedPublic)
            {
                return ModifierType.Public;
            }
            return ModifierType.Protected;
        }

        private ModifierType ComputeModifierType(FieldAttributes access)
        {
            if (access == FieldAttributes.Private || access == FieldAttributes.PrivateScope)
            {
                return ModifierType.Private;
            }
            if (access == FieldAttributes.Public)
            {
                return ModifierType.Public;
            }
            return ModifierType.Protected;
        }

        //private ModifierType ComputeModifierType(MethodAttributes access)
        //{
        //    if (access == MethodAttributes.Private || access == MethodAttributes.PrivateScope)
        //    {
        //        return ModifierType.Private;
        //    }
        //    if (access == MethodAttributes.Public)
        //    {
        //        return ModifierType.Public;
        //    }
        //    return ModifierType.Protected;
        //}

        private bool MatchModifier(ModifierType? modifierType, TypeDef typeDef)
        {
            return modifierType == null || (modifierType & ComputeModifierType(typeDef.Visibility)) != 0;
        }

        private bool MatchModifier(ModifierType? modifierType, FieldDef fieldDef)
        {
            return modifierType == null || (modifierType & ComputeModifierType(fieldDef.Access)) != 0;
        }

        private bool MatchModifier(ModifierType? modifierType, MethodDef methodDef)
        {
            return modifierType == null || (modifierType & ComputeModifierType((FieldAttributes)methodDef.Access)) != 0;
        }

        private bool MatchModifier(ModifierType? modifierType, PropertyDef propertyDef)
        {
            return modifierType == null || (modifierType & ComputeModifierType((FieldAttributes)propertyDef.Attributes)) != 0;
        }

        private bool MatchModifier(ModifierType? modifierType, EventDef eventDef)
        {
            return modifierType == null || (modifierType & ComputeModifierType((FieldAttributes)eventDef.Attributes)) != 0;
        }

        private class MethodComputeCache
        {
            public bool obfuscateName = true;
            public bool obfuscateParam = true;
            public bool obfuscateBody = true;
        }

        private class RuleResult
        {
            public bool? obfuscateName;
        }

        private readonly Dictionary<TypeDef, RuleResult> _typeSpecCache = new Dictionary<TypeDef, RuleResult>();
        private readonly Dictionary<MethodDef, RuleResult> _methodSpecCache = new Dictionary<MethodDef, RuleResult>();
        private readonly Dictionary<FieldDef, RuleResult> _fieldSpecCache = new Dictionary<FieldDef, RuleResult>();
        private readonly Dictionary<PropertyDef, RuleResult> _propertySpecCache = new Dictionary<PropertyDef, RuleResult>();
        private readonly Dictionary<EventDef, RuleResult> _eventSpecCache = new Dictionary<EventDef, RuleResult>();


        private readonly HashSet<string> _obfuscationAssemblyNames;
        private readonly List<ModuleDef> _assembliesToObfuscate;

        public ConfigurableRenamePolicy(List<string> obfuscationAssemblyNames, List<ModuleDef> assembliesToObfuscate, List<string> xmlFiles)
        {
            _obfuscationAssemblyNames = new HashSet<string>(obfuscationAssemblyNames);
            _assembliesToObfuscate = assembliesToObfuscate;
            LoadXmls(xmlFiles);
            BuildRuleResultCaches();
        }

        private bool MatchClassType(ClassType? classType, TypeDef typeDef)
        {
            if (classType == null)
            {
                return true;
            }
            if (typeDef.IsInterface && (classType & ClassType.Interface) != 0)
            {
                return true;
            }
            if (typeDef.IsEnum && (classType & ClassType.Enum) != 0)
            {
                return true;
            }
            if (typeDef.IsDelegate && (classType & ClassType.Delegate) != 0)
            {
                return true;
            }
            if (typeDef.IsValueType && !typeDef.IsEnum && (classType & ClassType.Struct) != 0)
            {
                return true;
            }
            if (!typeDef.IsValueType && !typeDef.IsInterface && !typeDef.IsDelegate && (classType & ClassType.Class) != 0)
            {
                return true;
            }
            return false;
        }


        private RuleResult GetOrCreateTypeRuleResult(TypeDef typeDef)
        {
            if (!_typeSpecCache.TryGetValue(typeDef, out var ruleResult))
            {
                ruleResult = new RuleResult();
                _typeSpecCache.Add(typeDef, ruleResult);
            }
            return ruleResult;
        }

        private RuleResult GetOrCreateFieldRuleResult(FieldDef field)
        {
            if (!_fieldSpecCache.TryGetValue(field, out var ruleResult))
            {
                ruleResult = new RuleResult();
                _fieldSpecCache.Add(field, ruleResult);
            }
            return ruleResult;
        }

        private RuleResult GetOrCreateMethodRuleResult(MethodDef method)
        {
            if (!_methodSpecCache.TryGetValue(method, out var ruleResult))
            {
                ruleResult = new RuleResult();
                _methodSpecCache.Add(method, ruleResult);
            }
            return ruleResult;
        }

        private RuleResult GetOrCreatePropertyRuleResult(PropertyDef property)
        {
            if (!_propertySpecCache.TryGetValue(property, out var ruleResult))
            {
                ruleResult = new RuleResult();
                _propertySpecCache.Add(property, ruleResult);
            }
            return ruleResult;
        }

        private RuleResult GetOrCreateEventRuleResult(EventDef eventDef)
        {
            if (!_eventSpecCache.TryGetValue(eventDef, out var ruleResult))
            {
                ruleResult = new RuleResult();
                _eventSpecCache.Add(eventDef, ruleResult);
            }
            return ruleResult;
        }

        private void BuildTypeRuleResult(TypeRuleSpec typeSpec, TypeDef typeDef, RuleResult typeRuleResult)
        {
            string typeName = typeDef.FullName;
        
            if (typeSpec.obfuscateName != null)
            {
                typeRuleResult.obfuscateName = typeSpec.obfuscateName;
            }

            foreach (var fieldDef in typeDef.Fields)
            {
                RuleResult fieldRuleResult = GetOrCreateFieldRuleResult(fieldDef);
                if (typeSpec.applyToMembers && typeSpec.obfuscateName != null)
                {
                    fieldRuleResult.obfuscateName = typeSpec.obfuscateName;
                }
                foreach (var fieldSpec in typeSpec.fields)
                {
                    if (fieldSpec.nameMatcher.IsMatch(fieldDef.Name) && MatchModifier(fieldSpec.modifierType, fieldDef))
                    {
                        if (fieldSpec.obfuscateName != null)
                        {
                            fieldRuleResult.obfuscateName = fieldSpec.obfuscateName;
                        }
                    }
                }
            }

            foreach (MethodDef methodDef in typeDef.Methods)
            {
                RuleResult methodRuleResult = GetOrCreateMethodRuleResult(methodDef);
                if (typeSpec.applyToMembers && typeSpec.obfuscateName != null)
                {
                    methodRuleResult.obfuscateName = typeSpec.obfuscateName;
                }
            }

            foreach (var eventDef in typeDef.Events)
            {
                RuleResult eventRuleResult = GetOrCreateEventRuleResult(eventDef);
                if (typeSpec.applyToMembers && typeSpec.obfuscateName != null)
                {
                    eventRuleResult.obfuscateName = typeSpec.obfuscateName;
                }
                foreach (var eventSpec in typeSpec.events)
                {
                    if (!eventSpec.nameMatcher.IsMatch(eventDef.Name) || !MatchModifier(eventSpec.modifierType, eventDef))
                    {
                        continue;
                    }
                    if (eventSpec.obfuscateName != null)
                    {
                        eventRuleResult.obfuscateName = eventSpec.obfuscateName;
                        if (eventSpec.applyToMethods)
                        {
                            if (eventDef.AddMethod != null)
                            {
                                GetOrCreateMethodRuleResult(eventDef.AddMethod).obfuscateName = eventSpec.obfuscateName;
                            }
                            if (eventDef.RemoveMethod != null)
                            {
                                GetOrCreateMethodRuleResult(eventDef.RemoveMethod).obfuscateName = eventSpec.obfuscateName;
                            }
                            if (eventDef.InvokeMethod != null)
                            {
                                GetOrCreateMethodRuleResult(eventDef.InvokeMethod).obfuscateName = eventSpec.obfuscateName;
                            }
                        }
                    }
                }
            }
            foreach (var propertyDef in typeDef.Properties)
            {
                RuleResult propertyRuleResult = GetOrCreatePropertyRuleResult(propertyDef);
                foreach (var propertySpec in typeSpec.properties)
                {
                    if (!propertySpec.nameMatcher.IsMatch(propertyDef.Name) || !MatchModifier(propertySpec.modifierType, propertyDef))
                    {
                        continue;
                    }
                    if (propertySpec.obfuscateName != null)
                    {
                        propertyRuleResult.obfuscateName = propertySpec.obfuscateName;
                        if (propertySpec.applyToMethods)
                        {
                            if (propertyDef.GetMethod != null)
                            {
                                GetOrCreateMethodRuleResult(propertyDef.GetMethod).obfuscateName = propertySpec.obfuscateName;
                            }
                            if (propertyDef.SetMethod != null)
                            {
                                GetOrCreateMethodRuleResult(propertyDef.SetMethod).obfuscateName = propertySpec.obfuscateName;
                            }
                        }
                    }
                }
            }
            foreach (MethodDef methodDef in typeDef.Methods)
            {
                RuleResult methodRuleResult = GetOrCreateMethodRuleResult(methodDef);
                foreach (MethodRuleSpec methodSpec in typeSpec.methods)
                {
                    if (!methodSpec.nameMatcher.IsMatch(methodDef.Name) || !MatchModifier(methodSpec.modifierType, methodDef))
                    {
                        continue;
                    }
                    if (methodSpec.obfuscateName != null)
                    {
                        methodRuleResult.obfuscateName = methodSpec.obfuscateName;
                    }
                }
            }

            foreach (TypeDef nestedType in typeDef.NestedTypes)
            {
                var nestedRuleResult = GetOrCreateTypeRuleResult(nestedType);
                if (typeSpec.applyToMembers && typeSpec.obfuscateName != null)
                {
                    nestedRuleResult.obfuscateName = typeSpec.obfuscateName;
                }
            }
        }

        private IEnumerable<TypeDef> GetMatchTypes(ModuleDef mod, List<TypeDef> types, TypeRuleSpec typeSpec)
        {
            if (typeSpec.nameMatcher.IsWildcardPattern)
            {
                foreach (var typeDef in types)
                {
                    if (!typeSpec.nameMatcher.IsMatch(typeDef.FullName) || !MatchModifier(typeSpec.modifierType, typeDef) || !MatchClassType(typeSpec.classType, typeDef))
                    {
                        continue;
                    }
                    yield return typeDef;
                }
            }
            else
            {
                TypeDef typeDef = mod.FindNormal(typeSpec.nameMatcher.NameOrPattern);
                if (typeDef != null && MatchModifier(typeSpec.modifierType, typeDef) && MatchClassType(typeSpec.classType, typeDef))
                {
                    yield return typeDef;
                }
            }
        }

        private void BuildRuleResultCaches()
        {
            foreach (AssemblyRuleSpec assSpec in _assemblyRuleSpecs.Values.SelectMany(arr => arr))
            {
                ModuleDef module = _assembliesToObfuscate.FirstOrDefault(m => m.Assembly.Name == assSpec.assemblyName);
                List<TypeDef> types = module.GetTypes().ToList();
                foreach (TypeRuleSpec typeSpec in assSpec.types)
                {
                    foreach (var typeDef in GetMatchTypes(module, types, typeSpec))
                    {
                        var ruleResult = GetOrCreateTypeRuleResult(typeDef);
                        if (typeSpec.obfuscateName != null)
                        {
                            ruleResult.obfuscateName = typeSpec.obfuscateName;
                        }
                        BuildTypeRuleResult(typeSpec, typeDef, ruleResult);
                    }
                }
            }
        }

        public override bool NeedRename(TypeDef typeDef)
        {
            return GetOrCreateTypeRuleResult(typeDef).obfuscateName != false;
        }

        public override bool NeedRename(MethodDef methodDef)
        {
            return GetOrCreateMethodRuleResult(methodDef).obfuscateName != false;
        }

        public override bool NeedRename(FieldDef fieldDef)
        {
            return GetOrCreateFieldRuleResult(fieldDef).obfuscateName != false;
        }

        public override bool NeedRename(PropertyDef propertyDef)
        {
            return GetOrCreatePropertyRuleResult(propertyDef).obfuscateName != false;
        }

        public override bool NeedRename(EventDef eventDef)
        {
            return GetOrCreateEventRuleResult(eventDef).obfuscateName != false;
        }
    }
}
