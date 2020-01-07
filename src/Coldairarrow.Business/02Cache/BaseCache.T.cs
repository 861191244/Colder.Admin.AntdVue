﻿using Coldairarrow.Util;
using System.Collections.Generic;

namespace Coldairarrow.Business.Cache
{
    public abstract class BaseCache<T> : IBaseCache<T> where T : class
    {
        public BaseCache()
        {
            _moduleKey = GetType().FullName;
        }

        #region 私有成员

        protected string _moduleKey { get; }
        protected abstract T GetDbData(string key);
        protected string BuildKey(string idKey)
        {
            return $"{GlobalSwitch.ProjectName}_Cache_{_moduleKey}_{idKey}";
        }

        #endregion

        #region 外部接口

        public T GetCache(string idKey)
        {
            if (idKey.IsNullOrEmpty())
                return null;

            string cacheKey = BuildKey(idKey);
            var cache = CacheHelper.Cache.GetCache<T>(cacheKey);
            if (cache == null)
            {
                cache = GetDbData(idKey);
                if (cache != null)
                    CacheHelper.Cache.SetCache(cacheKey, cache);
            }

            return cache;
        }

        public void UpdateCache(string idKey)
        {
            CacheHelper.Cache.RemoveCache(BuildKey(idKey));
        }

        public void UpdateCache(List<string> idKeys)
        {
            idKeys.ForEach(x => UpdateCache(x));
        }

        #endregion
    }
}
