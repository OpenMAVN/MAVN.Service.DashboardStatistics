﻿using MAVN.Service.DashboardStatistics.Domain.Repositories;
using MAVN.Service.DashboardStatistics.MsSqlRepositories.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MAVN.Persistence.PostgreSQL.Legacy;

namespace MAVN.Service.DashboardStatistics.MsSqlRepositories.Repositories
{
    public class CustomerStatisticRepository : ICustomerRegistrationRepository
    {
        private readonly PostgreSQLContextFactory<DashboardStatisticsContext> _contextFactory;

        public CustomerStatisticRepository(PostgreSQLContextFactory<DashboardStatisticsContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<IReadOnlyDictionary<DateTime, int>> GetCountPerDayAsync(DateTime startDate, DateTime endDate, Guid[] partnerIds)
        {
            using (var context = _contextFactory.CreateDataContext())
            {
                var query = context.CustomerStatistics
                    .Where(o => startDate <= o.TimeStamp && o.TimeStamp <= endDate);

                if (partnerIds != null && partnerIds.Any())
                    query = query.Where(o => o.PartnerId.HasValue && partnerIds.Contains(o.PartnerId.Value));

                var items = await query
                    .GroupBy(o => o.TimeStamp.Date)
                    .Select(o => new { Date = o.Key, Count = o.Count() })
                    .ToDictionaryAsync(k => k.Date, v => v.Count);

                return items;
            }
        }

        public async Task<int> GetCountSync(DateTime endDate, Guid[] partnerIds)
        {
            using (var context = _contextFactory.CreateDataContext())
            {
                var query = context.CustomerStatistics
                    .Where(o => o.TimeStamp <= endDate);

                if (partnerIds != null && partnerIds.Any())
                    query = query.Where(o => o.PartnerId.HasValue && partnerIds.Contains(o.PartnerId.Value));

                var count = await query.CountAsync();
                return count;
            }
        }

        public async Task InsertIfNotExistsAsync(Guid customerId, Guid? partnerId, DateTime registrationDate)
        {
            using (var context = _contextFactory.CreateDataContext())
            {
                if (partnerId.HasValue)
                {
                    var alreadyExists = await context.CustomerStatistics.AnyAsync(x =>
                        x.CustomerId == customerId && x.PartnerId == partnerId);

                    if (alreadyExists)
                        return;
                }

                await context.AddAsync(new CustomerStatisticEntity
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    PartnerId = partnerId,
                    TimeStamp = registrationDate
                });

                await context.SaveChangesAsync();
            }
        }
    }
}
