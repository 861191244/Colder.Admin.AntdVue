﻿using Coldairarrow.Business.Cache;
using Coldairarrow.Entity.Base_Manage;
using Coldairarrow.Util;
using EFCore.Sharding;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Coldairarrow.Business.Base_Manage
{
    public class Base_UserBusiness : BaseBusiness<Base_User>, IBase_UserBusiness, ITransientDependency
    {
        readonly IOperator _operator;
        public Base_UserBusiness(IRepository repository, IBase_UserCache userCache, IOperator @operator)
            : base(repository)
        {
            _userCache = userCache;
            _operator = @operator;
        }
        IBase_UserCache _userCache { get; }
        protected override string _textField => "RealName";

        #region 外部接口

        public async Task<List<Base_UserDTO>> GetDataListAsync(Pagination pagination, bool all, string userId = null, string keyword = null)
        {
            Expression<Func<Base_User, Base_Department, Base_UserDTO>> select = (a, b) => new Base_UserDTO
            {
                DepartmentName = b.Name
            };
            select = select.BuildExtendSelectExpre();
            var q_User = all ? Service.GetIQueryable<Base_User>() : GetIQueryable();
            var q = from a in q_User.AsExpandable()
                    join b in Service.GetIQueryable<Base_Department>() on a.DepartmentId equals b.Id into ab
                    from b in ab.DefaultIfEmpty()
                    select @select.Invoke(a, b);

            var where = LinqHelper.True<Base_UserDTO>();
            if (!userId.IsNullOrEmpty())
                where = where.And(x => x.Id == userId);
            if (!keyword.IsNullOrEmpty())
            {
                where = where.And(x =>
                    EF.Functions.Like(x.UserName, keyword)
                    || EF.Functions.Like(x.RealName, keyword));
            }

            var list = await q.Where(where).GetPagination(pagination).ToListAsync();

            await SetProperty(list);

            return list;

            async Task SetProperty(List<Base_UserDTO> users)
            {
                //补充用户角色属性
                List<string> userIds = users.Select(x => x.Id).ToList();
                var userRoles = await (from a in Service.GetIQueryable<Base_UserRole>()
                                       join b in Service.GetIQueryable<Base_Role>() on a.RoleId equals b.Id
                                       where userIds.Contains(a.UserId)
                                       select new
                                       {
                                           a.UserId,
                                           RoleId = b.Id,
                                           b.RoleName
                                       }).ToListAsync();
                users.ForEach(aUser =>
                {
                    var roleList = userRoles.Where(x => x.UserId == aUser.Id);
                    aUser.RoleIdList = roleList.Select(x => x.RoleId).ToList();
                    aUser.RoleNameList = roleList.Select(x => x.RoleName).ToList();
                });
            }
        }

        public async Task<Base_UserDTO> GetTheDataAsync(string id)
        {
            if (id.IsNullOrEmpty())
                return null;
            else
                return (await GetDataListAsync(new Pagination(), true, id)).FirstOrDefault();
        }

        [DataAddLog(UserLogType.系统用户管理, "RealName", "用户")]
        [DataRepeatValidate(
            new string[] { "UserName" },
            new string[] { "用户名" })]
        [Transactional]
        public async Task AddDataAsync(Base_User newData, List<string> roleIds)
        {
            await InsertAsync(newData);
            await SetUserRoleAsync(newData.Id, roleIds);
        }

        [DataEditLog(UserLogType.系统用户管理, "RealName", "用户")]
        [DataRepeatValidate(
            new string[] { "UserName" },
            new string[] { "用户名" })]
        [Transactional]
        public async Task UpdateDataAsync(Base_User theData, List<string> roleIds)
        {
            if (theData.Id == GlobalData.ADMINID && _operator?.UserId != theData.Id)
                throw new BusException("禁止更改超级管理员！");

            await UpdateAsync(theData);
            await SetUserRoleAsync(theData.Id, roleIds);
            await _userCache.UpdateCacheAsync(theData.Id);
        }

        [DataDeleteLog(UserLogType.系统用户管理, "RealName", "用户")]
        [Transactional]
        public async Task DeleteDataAsync(List<string> ids)
        {
            if (ids.Contains(GlobalData.ADMINID))
                throw new BusException("超级管理员是内置账号,禁止删除！");
            var userIds = await GetIQueryable().Where(x => ids.Contains(x.Id)).Select(x => x.Id).ToListAsync();

            await DeleteAsync(ids);

            await _userCache.UpdateCacheAsync(ids);
        }

        #endregion

        #region 私有成员

        private async Task SetUserRoleAsync(string userId, List<string> roleIds)
        {
            var userRoleList = roleIds.Select(x => new Base_UserRole
            {
                Id = IdHelper.GetId(),
                CreateTime = DateTime.Now,
                UserId = userId,
                RoleId = x
            }).ToList();
            await Service.Delete_SqlAsync<Base_UserRole>(x => x.UserId == userId);
            await Service.InsertAsync(userRoleList);
        }

        #endregion
    }
}