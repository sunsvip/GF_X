using dnlib.DotNet;
using Obfuz.Conf;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

namespace Obfuz.ObfusPasses.CallObfus
{
    public class ConfigurableObfuscationPolicy : ObfuscationPolicyBase
    {
        class WhiteListAssembly
        {
            public string name;
            public NameMatcher nameMatcher;
            public bool? obfuscate;
            public List<WhiteListType> types = new List<WhiteListType>();
        }

        class WhiteListType
        {
            public string name;
            public NameMatcher nameMatcher;
            public bool? obfuscate;
            public List<WhiteListMethod> methods = new List<WhiteListMethod>();
        }

        class WhiteListMethod
        {
            public string name;
            public NameMatcher nameMatcher;
            public bool? obfuscate;
        }

        class ObfuscationRule : IRule<ObfuscationRule>
        {
            public bool? disableObfuscation;
            public bool? obfuscateCallInLoop;
            public bool? cacheCallIndexInLoop;
            public bool? cacheCallIndexNotLoop;

            public void InheritParent(ObfuscationRule parentRule)
            {
                if (disableObfuscation == null)
                {
                    disableObfuscation = parentRule.disableObfuscation;
                }
                if (obfuscateCallInLoop == null)
                {
                    obfuscateCallInLoop = parentRule.obfuscateCallInLoop;
                }
                if (cacheCallIndexInLoop == null)
                {
                    cacheCallIndexInLoop = parentRule.cacheCallIndexInLoop;
                }
                if (cacheCallIndexNotLoop == null)
                {
                    cacheCallIndexNotLoop = parentRule.cacheCallIndexNotLoop;
                }
            }
        }

        class AssemblySpec : AssemblyRuleBase<TypeSpec, MethodSpec, ObfuscationRule>
        {
        }

        class TypeSpec : TypeRuleBase<MethodSpec, ObfuscationRule>
        {
        }

        class MethodSpec : MethodRuleBase<ObfuscationRule>
        {

        }

        private static readonly ObfuscationRule s_default = new ObfuscationRule()
        {
            disableObfuscation = false,
            obfuscateCallInLoop = true,
            cacheCallIndexInLoop = true,
            cacheCallIndexNotLoop = false,
        };

        private readonly XmlAssemblyTypeMethodRuleParser<AssemblySpec, TypeSpec, MethodSpec, ObfuscationRule> _configParser;

        private ObfuscationRule _global;
        private readonly List<WhiteListAssembly> _whiteListAssemblies = new List<WhiteListAssembly>();

        private readonly Dictionary<IMethod, bool> _whiteListMethodCache = new Dictionary<IMethod, bool>(MethodEqualityComparer.CompareDeclaringTypes);
        private readonly Dictionary<MethodDef, ObfuscationRule> _methodRuleCache = new Dictionary<MethodDef, ObfuscationRule>();

        public ConfigurableObfuscationPolicy(List<string> toObfuscatedAssemblyNames, List<string> xmlConfigFiles)
        {
            _configParser = new XmlAssemblyTypeMethodRuleParser<AssemblySpec, TypeSpec, MethodSpec, ObfuscationRule>(toObfuscatedAssemblyNames,
                ParseObfuscationRule, ParseGlobalElement);
            LoadConfigs(xmlConfigFiles);
        }

        private void LoadConfigs(List<string> configFiles)
        {
            _configParser.LoadConfigs(configFiles);

            if (_global == null)
            {
                _global = s_default;
            }
            else
            {
                _global.InheritParent(s_default);
            }
            _configParser.InheritParentRules(_global);
            InheritWhitelistRules();
        }

        private void InheritWhitelistRules()
        {
            foreach (var ass in _whiteListAssemblies)
            {
                foreach (var type in ass.types)
                {
                    if (type.obfuscate == null)
                    {
                        type.obfuscate = ass.obfuscate;
                    }
                    foreach (var method in type.methods)
                    {
                        if (method.obfuscate == null)
                        {
                            method.obfuscate = type.obfuscate;
                        }
                    }
                }
            }
        }

        private void ParseGlobalElement(string configFile, XmlElement ele)
        {
            switch (ele.Name)
            {
                case "global": _global = ParseObfuscationRule(configFile, ele); break;
                case "whitelist": ParseWhitelist(ele); break;
                default: throw new Exception($"Invalid xml file {configFile}, unknown node {ele.Name}");
            }
        }

        private ObfuscationRule ParseObfuscationRule(string configFile, XmlElement ele)
        {
            var rule = new ObfuscationRule();
            if (ele.HasAttribute("disableObfuscation"))
            {
                rule.disableObfuscation = ConfigUtil.ParseBool(ele.GetAttribute("disableObfuscation"));
            }
            if (ele.HasAttribute("obfuscateCallInLoop"))
            {
                rule.obfuscateCallInLoop = ConfigUtil.ParseBool(ele.GetAttribute("obfuscateCallInLoop"));
            }
            if (ele.HasAttribute("cacheCallIndexInLoop"))
            {
                rule.cacheCallIndexInLoop = ConfigUtil.ParseBool(ele.GetAttribute("cacheCallIndexInLoop"));
            }
            if (ele.HasAttribute("cacheCallIndexNotLoop"))
            {
                rule.cacheCallIndexNotLoop = ConfigUtil.ParseBool(ele.GetAttribute("cacheCallIndexNotLoop"));
            }
            return rule;
        }

        private void ParseWhitelist(XmlElement ruleEle)
        {
            foreach (XmlNode xmlNode in ruleEle.ChildNodes)
            {
                if (!(xmlNode is XmlElement childEle))
                {
                    continue;
                }
                switch (childEle.Name)
                {
                    case "assembly":
                    {
                        var ass = ParseWhiteListAssembly(childEle);
                        _whiteListAssemblies.Add(ass);
                        break;
                    }
                    default: throw new Exception($"Invalid xml file, unknown node {childEle.Name}");
                }
            }
        }

        private WhiteListAssembly ParseWhiteListAssembly(XmlElement element)
        {
            var ass = new WhiteListAssembly();
            ass.name = element.GetAttribute("name");
            ass.nameMatcher = new NameMatcher(ass.name);

            ass.obfuscate = ConfigUtil.ParseNullableBool(element.GetAttribute("obfuscate")) ?? false;

            foreach (XmlNode node in element.ChildNodes)
            {
                if (!(node is XmlElement ele))
                {
                    continue;
                }
                switch (ele.Name)
                {
                    case "type":
                    ass.types.Add(ParseWhiteListType(ele));
                    break;
                    default:
                    throw new Exception($"Invalid xml file, unknown node {ele.Name}");
                }
            }
            return ass;
        }

        private WhiteListType ParseWhiteListType(XmlElement element)
        {
            var type = new WhiteListType();
            type.name = element.GetAttribute("name");
            type.nameMatcher = new NameMatcher(type.name);
            type.obfuscate = ConfigUtil.ParseNullableBool(element.GetAttribute("obfuscate"));

            foreach (XmlNode node in element.ChildNodes)
            {
                if (!(node is XmlElement ele))
                {
                    continue;
                }
                switch (ele.Name)
                {
                    case "method":
                    {
                        type.methods.Add(ParseWhiteListMethod(ele));
                        break;
                    }
                    default: throw new Exception($"Invalid xml file, unknown node {ele.Name}");
                }
            }

            return type;
        }

        private WhiteListMethod ParseWhiteListMethod(XmlElement element)
        {
            var method = new WhiteListMethod();
            method.name = element.GetAttribute("name");
            method.nameMatcher = new NameMatcher(method.name);
            method.obfuscate = ConfigUtil.ParseNullableBool(element.GetAttribute("obfuscate"));
            return method;
        }

        private ObfuscationRule GetMethodObfuscationRule(MethodDef method)
        {
            if (!_methodRuleCache.TryGetValue(method, out var rule))
            {
                rule = _configParser.GetMethodRule(method, s_default);
                _methodRuleCache[method] = rule;
            }
            return rule;
        }

        public override bool NeedObfuscateCallInMethod(MethodDef method)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            return rule.disableObfuscation != true;
        }

        public override ObfuscationCachePolicy GetMethodObfuscationCachePolicy(MethodDef method)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            return new ObfuscationCachePolicy()
            {
                cacheInLoop = rule.cacheCallIndexInLoop.Value,
                cacheNotInLoop = rule.cacheCallIndexNotLoop.Value,
            };
        }


        private bool IsSpecialNotObfuscatedMethod(TypeDef typeDef, IMethod method)
        {
            if (typeDef.IsDelegate || typeDef.IsEnum)
                return true;

            string methodName = method.Name;

            // doesn't proxy call if the method is a constructor
            if (methodName == ".ctor")
            {
                return true;
            }

            if (typeDef.Name == "EncryptionService`1")
            {
                return true;
            }
            // special handle
            // don't proxy call for List<T>.Enumerator GetEnumerator()
            if (methodName == "GetEnumerator")
            {
                return true;
            }
            return false;
        }

        private bool ComputeIsInWhiteList(IMethod calledMethod)
        {
            ITypeDefOrRef declaringType = calledMethod.DeclaringType;
            TypeSig declaringTypeSig = calledMethod.DeclaringType.ToTypeSig();
            declaringTypeSig = declaringTypeSig.RemovePinnedAndModifiers();
            switch (declaringTypeSig.ElementType)
            {
                case ElementType.ValueType:
                case ElementType.Class:
                {
                    break;
                }
                case ElementType.GenericInst:
                {
                    if (MetaUtil.ContainsContainsGenericParameter(calledMethod))
                    {
                        return true;
                    }
                    break;
                }
                default: return true;
            }

            TypeDef typeDef = declaringType.ResolveTypeDef();

            if (IsSpecialNotObfuscatedMethod(typeDef, calledMethod))
            {
                return true;
            }

            string assName = typeDef.Module.Assembly.Name;
            string typeFullName = typeDef.FullName;
            string methodName = calledMethod.Name;
            foreach (var ass in _whiteListAssemblies)
            {
                if (!ass.nameMatcher.IsMatch(assName))
                {
                    continue;
                }
                foreach (var type in ass.types)
                {
                    if (!type.nameMatcher.IsMatch(typeFullName))
                    {
                        continue;
                    }
                    foreach (var method in type.methods)
                    {
                        if (method.nameMatcher.IsMatch(methodName))
                        {
                            return !method.obfuscate.Value;
                        }
                    }
                    return !type.obfuscate.Value;
                }
                return !ass.obfuscate.Value;
            }
            return false;
        }

        private bool IsInWhiteList(IMethod method)
        {
            if (!_whiteListMethodCache.TryGetValue(method, out var isWhiteList))
            {
                isWhiteList = ComputeIsInWhiteList(method);
                _whiteListMethodCache.Add(method, isWhiteList);
            }
            return isWhiteList;
        }

        private bool IsTypeSelfAndParentPublic(TypeDef type)
        {
            if (type.DeclaringType != null && !IsTypeSelfAndParentPublic(type.DeclaringType))
            {
                return false;
            }

            return type.IsPublic;
        }

        public override bool NeedObfuscateCalledMethod(MethodDef callerMethod, IMethod calledMethod, bool callVir, bool currentInLoop)
        {
            if (IsInWhiteList(calledMethod))
            {
                return false;
            }

            // mono has more strict access control, calls non-public method will raise exception.
            if (PlatformUtil.IsMonoBackend())
            {
                MethodDef calledMethodDef = calledMethod.ResolveMethodDef();
                if (calledMethodDef != null && (!calledMethodDef.IsPublic || !IsTypeSelfAndParentPublic(calledMethodDef.DeclaringType)))
                {
                    return false;
                }
            }
            ObfuscationRule rule = GetMethodObfuscationRule(callerMethod);
            if (currentInLoop && rule.obfuscateCallInLoop == false)
            {
                return false;
            }
            return true;
        }
    }
}
