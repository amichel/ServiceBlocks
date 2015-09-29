using System;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace ServiceBlocks.Common.Serializers
{
    public sealed class IgnoreAssemblyVersionBinder : SerializationBinder
    {
        private const string VERSION_REGEX = @"Version=.*?, ";
        private const string PublicKeyToken_REGEX = @", PublicKeyToken=\w*";

        private static readonly Regex m_regex_findversiontag = new Regex(VERSION_REGEX, RegexOptions.Compiled);
        private static readonly Regex m_regex_findpublictokentag = new Regex(PublicKeyToken_REGEX, RegexOptions.Compiled);

        private readonly bool m_IgnorePublicKeyToken;
        private readonly bool m_IgnoreVersion;

        public IgnoreAssemblyVersionBinder() : this(true, false)
        {
        }

        public IgnoreAssemblyVersionBinder(bool ignoreVersion, bool ignorePublicKeyToken)
        {
            m_IgnoreVersion = ignoreVersion;
            m_IgnorePublicKeyToken = ignorePublicKeyToken;
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
            if (m_IgnoreVersion)
            {
                typeName = m_regex_findversiontag.Replace(typeName, string.Empty);
                assemblyName = m_regex_findversiontag.Replace(assemblyName, string.Empty);
            }
            if (m_IgnorePublicKeyToken)
            {
                typeName = m_regex_findpublictokentag.Replace(typeName, string.Empty);
                assemblyName = m_regex_findpublictokentag.Replace(typeName, string.Empty);
            }
            Type type;
            if (ValidateType(typeName, out type))
            {
                return type;
            }
            return null;
        }

        private bool ValidateType(string typeName, out Type type)
        {
            type = Type.GetType(typeName);
            return true;
        }
    }
}