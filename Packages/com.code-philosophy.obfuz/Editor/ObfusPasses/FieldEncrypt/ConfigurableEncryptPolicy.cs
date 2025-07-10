using dnlib.DotNet;
using Obfuz.Conf;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Obfuz.ObfusPasses.FieldEncrypt
{
    public class ConfigurableEncryptPolicy : EncryptPolicyBase
    {
        class ObfuscationRule
        {

        }

        private readonly XmlFieldRuleParser<ObfuscationRule> _configParser;

        public ConfigurableEncryptPolicy(List<string> toObfuscatedAssemblyNames, List<string> configFiles)
        {
            _configParser = new XmlFieldRuleParser<ObfuscationRule>(toObfuscatedAssemblyNames, ParseRule, null);
            _configParser.LoadConfigs(configFiles);
        }

        private ObfuscationRule ParseRule(string configFile, XmlElement ele)
        {
            return new ObfuscationRule();
        }

        public override bool NeedEncrypt(FieldDef field)
        {
            if (MetaUtil.HasEncryptFieldAttribute(field))
            {
                return true;
            }
            if (MetaUtil.HasSelfOrInheritObfuzIgnoreScope(field, field.DeclaringType, ObfuzScope.Field))
            {
                return false;
            }
            var rule = _configParser.GetFieldRule(field);
            return rule != null;
        }
    }
}
