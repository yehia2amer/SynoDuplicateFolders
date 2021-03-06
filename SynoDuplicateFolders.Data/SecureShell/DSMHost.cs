﻿using System.Collections.Generic;
using System.Configuration;
using SynoDuplicateFolders.Configuration;

namespace SynoDuplicateFolders.Data.SecureShell
{
    public class DSMHost : ConfigurationElement, IElementProvider
    {
        [ConfigurationProperty("host", IsRequired = true, IsKey = true)]
        public string Host
        {
            get
            {
                return this["host"] as string;
            }
            set
            {
                this["host"] = value;
            }
        }

        [ConfigurationProperty("port", IsRequired = false, DefaultValue = 22)]
        public int Port
        {
            get
            {
                return (int)this["port"];
            }
            set
            {
                this["port"] = value;
            }
        }

        [ConfigurationProperty("user", IsRequired = true)]
        public string UserName
        {
            get
            {
                return this["user"] as string;
            }
            set
            {
                this["user"] = value;
            }
        }
        [ConfigurationProperty("home", IsRequired = false)]
        public string SynoReportHome
        {
            get
            {
                return this["home"] as string;
            }
            set
            {
                this["home"] = value;
            }
        }
              
        [ConfigurationProperty("AuthenticationMethods")]
        public BasicConfigurationElementMap<DSMAuthentication> AuthenticationSection
        {
            get
            {
                return this["AuthenticationMethods"] as BasicConfigurationElementMap<DSMAuthentication>;
            }
        }
        internal IEnumerable<DSMAuthentication> AuthenticationMethods
        {
            get
            {
                foreach (DSMAuthentication am in AuthenticationSection)
                {
                    yield return am;
                }
            }
        }
        public DSMAuthentication GetAuthenticationMethod(DSMAuthenticationMethod method)
        {
            if (AuthenticationSection.ContainsKey(method))
            {
                return AuthenticationSection[method];
            }
            else
            {                
                return null;
            }
        }        

        public DSMAuthentication GetOrAddAuthenticationMethod(DSMAuthenticationMethod method)
        {
            if (AuthenticationSection.ContainsKey(method))
            {
                return AuthenticationSection[method];
            }
            else
            {
                var added = new DSMAuthentication(method);
                AuthenticationSection.Add(added);
                return added;
            }
        }
        public DSMAuthentication UpdateAuthenticationMethod(DSMAuthenticationMethod method, bool add)
        {
            if (!add) RemoveAuthenticationMethod(method);
            return add ? GetOrAddAuthenticationMethod(method) : null;
        }
        public void RemoveAuthenticationMethod(DSMAuthenticationMethod method)
        {
            if (AuthenticationSection.ContainsKey(method))
            {
                AuthenticationSection.Remove(AuthenticationSection[method]);
            }
        }
        public bool HasAuthenticationMethod(DSMAuthenticationMethod method)
        {
            return AuthenticationSection.ContainsKey(method);
        }

        public DSMProxy Proxy
        {
            get
            {
                if (ProxyBacking.Count == 0)
                {
                    return null;
                }
                else
                {
                    return ProxyBacking[0];
                }
            }
            set
            {
                ProxyBacking.Clear();
                ProxyBacking.Add(value);
            }
        }
        [ConfigurationProperty("Proxy")]
        private BasicConfigurationElementMap<DSMProxy> ProxyBacking
        {
            get
            {
                return this["Proxy"] as BasicConfigurationElementMap<DSMProxy>;
            }
        }

        object IElementProvider.GetElementKey()
        {
            return Host;
        }

        string IElementProvider.GetElementName()
        {
            return "DSMHost";
        }
    }
}
