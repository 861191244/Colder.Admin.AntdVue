﻿using Coldairarrow.Entity.Base_Manage;
using Coldairarrow.Util;
using EFCore.Sharding;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Coldairarrow.Business.Base_Manage
{
    public class Base_AppSecretBusiness : BaseBusiness<Base_AppSecret>, IBase_AppSecretBusiness, ITransientDependency
    {
        public Base_AppSecretBusiness(IRepository repository) : base(repository)
        {
        }

        #region 外部接口

        public async Task<List<Base_AppSecret>> GetDataListAsync(Pagination pagination, string keyword)
        {
            var q = GetIQueryable();
            var where = LinqHelper.True<Base_AppSecret>();
            if (!keyword.IsNullOrEmpty())
            {
                where = where.And(x =>
                    x.AppId.Contains(keyword)
                    || x.AppSecret.Contains(keyword)
                    || x.AppName.Contains(keyword));
            }

            return await q.Where(where).GetPagination(pagination).ToListAsync();
        }

        public async Task<Base_AppSecret> GetTheDataAsync(string id)
        {
            return await GetEntityAsync(id);
        }

        public async Task<string> GetAppSecretAsync(string appId)
        {
            var theData = await GetIQueryable().Where(x => x.AppId == appId).FirstOrDefaultAsync();

            return theData?.AppSecret;
        }

        [DataRepeatValidate(new string[] { "AppId" },
            new string[] { "应用Id" })]
        [DataAddLog(UserLogType.接口密钥管理, "AppId", "应用Id")]
        public async Task AddDataAsync(Base_AppSecret newData)
        {
            await InsertAsync(newData);
        }

        [DataRepeatValidate(new string[] { "AppId" },
            new string[] { "应用Id" })]
        [DataEditLog(UserLogType.接口密钥管理, "AppId", "应用Id")]
        public async Task UpdateDataAsync(Base_AppSecret theData)
        {
            await UpdateAsync(theData);
        }

        [DataDeleteLog(UserLogType.接口密钥管理, "AppId", "应用Id")]
        public async Task DeleteDataAsync(List<string> ids)
        {
            await DeleteAsync(ids);
        }

        #endregion

        #region 私有成员

        #endregion
    }
}