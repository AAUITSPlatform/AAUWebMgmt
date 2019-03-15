﻿using ITSWebMgmt.Caches;
using NLog;
using System.Collections.Generic;
using System.DirectoryServices;

namespace ITSWebMgmt.Controllers
{
    public class Controller<T> where T : ADcache
    {
        public T ADcache;
        public string Path { get => ADcache.Path; }
        public static Logger logger = LogManager.GetCurrentClassLogger();
        public virtual string adpath { get => ADcache.adpath; set { ADcache.adpath = value; } }
        public List<string> getGroups(string name) => ADcache.getGroups(name);
        public List<string> getGroupsTransitive(string name) => ADcache.getGroupsTransitive(name);
        public List<PropertyValueCollection> getAllProperties() => ADcache.getAllProperties();
    }
}