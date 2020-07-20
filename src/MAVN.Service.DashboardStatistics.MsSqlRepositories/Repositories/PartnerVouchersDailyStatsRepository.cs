﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MAVN.Persistence.PostgreSQL.Legacy;
using MAVN.Service.DashboardStatistics.Domain.Models.VoucherStatistic;
using MAVN.Service.DashboardStatistics.Domain.Repositories;
using MAVN.Service.DashboardStatistics.MsSqlRepositories.Entities;
using Microsoft.EntityFrameworkCore;

namespace MAVN.Service.DashboardStatistics.MsSqlRepositories.Repositories
{
    public class PartnerVouchersDailyStatsRepository : IPartnerVouchersDailyStatsRepository
    {
        private readonly PostgreSQLContextFactory<DashboardStatisticsContext> _contextFactory;

        public PartnerVouchersDailyStatsRepository(
            PostgreSQLContextFactory<DashboardStatisticsContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task UpdateByDateAndCurrencyAndOperationType(UpdateVoucherOperationsStatistic partnerStatistic)
        {
            var nowDate = DateTime.Now.Date;
            using (var context = _contextFactory.CreateDataContext())
            {
                var entity = await context.PartnerVouchersDailyStatistics
                    .FirstOrDefaultAsync(x =>
                        x.PartnerId == partnerStatistic.PartnerId
                        && x.Date == nowDate
                        && x.Currency == partnerStatistic.Currency
                        && x.OperationType == partnerStatistic.OperationType);

                if (entity != null)
                {
                    entity.TotalCount++;
                    entity.Sum += partnerStatistic.Amount;

                    context.Update(entity);
                }
                else
                {
                    var newEntity = new PartnerVouchersDailyStatsEntity
                    {
                        PartnerId = partnerStatistic.PartnerId,
                        Currency = partnerStatistic.Currency,
                        Sum = partnerStatistic.Amount,
                        OperationType = partnerStatistic.OperationType,
                        TotalCount = 1,
                        Date = nowDate,
                    };

                    context.PartnerVouchersDailyStatistics.Add(newEntity);
                }

                await context.SaveChangesAsync();
            }
        }

        public async Task<IReadOnlyList<IPartnerVouchersDailyStats>> GetByPartnerIdsAndPeriod(Guid[] partnerIds, DateTime fromDate, DateTime toDate)
        {
            using (var context = _contextFactory.CreateDataContext())
            {
                var query =  context.PartnerVouchersDailyStatistics
                    .Where(x => x.Date >= fromDate.Date && x.Date <= toDate.Date);

                if (partnerIds != null && partnerIds.Any())
                    query = query.Where(x => partnerIds.Contains(x.PartnerId));

                var result = await query.ToListAsync();
                return result;
            }
        }
    }
}
