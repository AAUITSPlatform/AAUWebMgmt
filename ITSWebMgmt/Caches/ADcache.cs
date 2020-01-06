using ITSWebMgmt.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.Linq;

namespace ITSWebMgmt.Caches
{
    public class Property
    {
        public string Name;
        public Type Type;
        public object Value;

        public Property(string name, Type type)
        {
            Name = name;
            Type = type;
        }
    }

    public abstract class ADcache
    {
        protected Dictionary<string, Property> properties = new Dictionary<string, Property>();
        public DirectoryEntry DE;
        public SearchResult result;
        public string Path { get => DE.Path; }
        public string adpath;
        private List<Type> types = new List<Type>();
        private List<PropertyValueCollection> AllProperties;
        private Dictionary<string, List<string>> groups = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> groupsTransitive = new Dictionary<string, List<string>>();
        private readonly object cacheLock = new object();
        public ADcache() { }

        public ADcache(string adpath, List<Property> properties, List<Property> propertiesToRefresh)
        {
            this.adpath = adpath;
            DE = DirectoryEntryCreator.CreateNewDirectoryEntry(adpath);
            var search = new DirectorySearcher(DE);

            if (propertiesToRefresh != null)
            {
                List<string> propertiesNamesToRefresh = new List<string>();
                foreach (var p in propertiesToRefresh)
                {
                    propertiesNamesToRefresh.Add(p.Name);
                    properties.Add(p);
                }

                DE.RefreshCache(propertiesNamesToRefresh.ToArray());
            }

            foreach (var p in properties)
            {
                search.PropertiesToLoad.Add(p.Name);
            }

            result = search.FindOne();

            saveCache(properties, propertiesToRefresh);
        }

        protected void saveCache(List<Property> properties, List<Property> propertiesToRefresh)
        {
            foreach (var p in properties)
            {
                var value = DE.Properties[p.Name].Value;
                //TODO Handle all null values here and give then default values
                if (value == null)
                {
                    if (p.Type.Equals(typeof(bool)))
                        value = false;
                    else if (p.Type.Equals(typeof(int)))
                        value = -1;
                    else if (p.Type.Equals(typeof(string)))
                        value = "";
                    else if (p.Type.Equals(typeof(object[])))
                        value = new string[] { "" };
                }
                else
                {
                    //Print the type
                    Debug.WriteLine($"{p.Name}: {value.GetType().ToString()}, value: {value.ToString()}");

                    //Handle special types
                    if (value.GetType().Equals(typeof(object[])))
                    {
                        value = ((object[])value).Cast<string>().ToArray();
                    }
                    if (value.GetType().ToString() == "System.__ComObject")
                    {
                        value = DateTimeConverter.Convert(value);
                    }
                }

                p.Value = value;
                addProperty(p.Name, p);
            }
        }

        public List<PropertyValueCollection> getAllProperties()
        {
            if (AllProperties == null)
            {
                AllProperties = new List<PropertyValueCollection>();
                foreach (string k in DE.Properties.PropertyNames)
                {
                    AllProperties.Add(DE.Properties[k]);
                }
            }
            return AllProperties;
        }

        public dynamic getProperty(string property)
        {
            if (properties.ContainsKey(property))
            {
                return properties[property].Value;
            }
            return null;
        }

        public void saveProperty(string property, dynamic value)
        {
            if (properties[property].Type.Equals(value.GetType()))
            {
                DE.Properties[property].Value = value;
                DE.CommitChanges();
            }
            else
                Debug.WriteLine($"Not saved due to wrong type");
        }

        public void clearProperty(string property)
        {
            DE.Properties[property].Clear();
            DE.CommitChanges();
        }

        public void addProperty(string property, Property value)
        {
            properties.Add(property, value);
        }

        public List<string> getGroups(string name)
        {
            if (!groups.ContainsKey(name))
            {
                groups.Add(name, result.Properties[name].Cast<string>().ToList());
            }
            return groups[name];
        }

        public List<string> getGroupsTransitive(string name)
        {
            string attName = $"msds-{name}Transitive";
            lock (cacheLock)
            {
                if (!groupsTransitive.ContainsKey(attName))
                {
                    DE.RefreshCache(attName.Split(','));
                    groupsTransitive.Add(attName, DE.Properties[attName].Cast<string>().ToList());
                }
            }
            return groupsTransitive[attName];
        }
    }
}