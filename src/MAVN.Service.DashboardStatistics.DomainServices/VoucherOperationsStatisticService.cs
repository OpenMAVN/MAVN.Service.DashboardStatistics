﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MAVN.Service.DashboardStatistics.Domain.Enums;
using MAVN.Service.DashboardStatistics.Domain.Models.VoucherStatistic;
using MAVN.Service.DashboardStatistics.Domain.Repositories;
using MAVN.Service.DashboardStatistics.Domain.Services;
namespace MAVN.Service.DashboardStatistics.DomainServices
{
    public class VoucherOperationsStatisticService : IVoucherOperationsStatisticService
    {
        private const int MaxAttemptsCount = 5;
        private readonly IVoucherOperationsStatisticRepository _voucherOperationsStatisticRepository;
        private readonly IPartnerVouchersDailyStatsRepository _partnerVouchersDailyStatsRepository;
        private readonly IRedisLocksService _redisLocksService;
        private readonly TimeSpan _lockTimeOut;

        public VoucherOperationsStatisticService(
            IVoucherOperationsStatisticRepository voucherOperationsStatisticRepository,
            IPartnerVouchersDailyStatsRepository partnerVouchersDailyStatsRepository,
            IRedisLocksService redisLocksService,
            TimeSpan lockTimeOut)
        {
            _voucherOperationsStatisticRepository = voucherOperationsStatisticRepository;
            _partnerVouchersDailyStatsRepository = partnerVouchersDailyStatsRepository;
            _redisLocksService = redisLocksService;
            _lockTimeOut = lockTimeOut;
        }

        public async Task UpdateVoucherOperationsStatistic(UpdateVoucherOperationsStatistic partnerStatistic)
        {
            var lockValue = $"{partnerStatistic.PartnerId}_{partnerStatistic.OperationType}_{partnerStatistic.Currency}";
            for (var i = 0; i < MaxAttemptsCount; ++i)
            {
                var locked = await _redisLocksService.TryAcquireLockAsync(
                    lockValue,
                    lockValue,
                    _lockTimeOut);

                if (!locked)
                {
                    await Task.Delay(_lockTimeOut);
                    continue;
                }

                await _voucherOperationsStatisticRepository.UpdateByCurrencyAndOperationType(partnerStatistic);
                await _partnerVouchersDailyStatsRepository.UpdateByDateAndCurrencyAndOperationType(partnerStatistic);

                await _redisLocksService.ReleaseLockAsync(lockValue, lockValue);
                return;
            }

            throw new InvalidOperationException("Couldn't acquire a lock in Redis");
        }

        public async Task<IList<CurrenciesStatistic>> GetCurrenciesStatistic(Guid[] partnerIds, bool filterByPartnerIds)
        {
            var statistic = await _voucherOperationsStatisticRepository.GetByPartnerIds(partnerIds, filterByPartnerIds);
            var currencyGroups = statistic
                .GroupBy(x => x.Currency);
            var currenciesStatistics = new List<CurrenciesStatistic>();
            foreach (var currencyGroup in currencyGroups)
            {
                var element = new CurrenciesStatistic { Currency = currencyGroup.Key };

                foreach (var operationsStatistic in currencyGroup)
                {
                    switch (operationsStatistic.OperationType)
                    {
                        case VoucherOperationType.Redeem:
                            element.TotalRedeemedVouchersCost += operationsStatistic.Amount;
                            element.TotalRedeemedVouchersCount += operationsStatistic.TotalCount;
                            break;
                        case VoucherOperationType.Buy:
                            element.TotalPurchasesCost += operationsStatistic.Amount;
                            element.TotalPurchasesCount += operationsStatistic.TotalCount;
                            break;
                    }
                }

                currenciesStatistics.Add(element);
            }

            return currenciesStatistics;
        }

        public async Task<VouchersDailyStatistics> GetPartnerDailyVoucherStatistic(Guid[] partnerIds, bool filterByPartnerIds, DateTime fromDate, DateTime toDate)
        {
            var statistic = await _partnerVouchersDailyStatsRepository.GetByPartnerIdsAndPeriod(partnerIds, filterByPartnerIds, fromDate, toDate);

            var result = new VouchersDailyStatistics
            {
                BoughtVoucherStatistics = new List<VoucherStatisticsModel>(),
                UsedVoucherStatistics = new List<VoucherStatisticsModel>(),
            };

            var currencyGroups = statistic
                .GroupBy(x => (x.Currency, x.Date));

            foreach (var currencyGroup in currencyGroups)
            {
                var bought = new VoucherStatisticsModel
                {
                    Currency = currencyGroup.Key.Currency,
                    Date = currencyGroup.Key.Date,
                };

                var used = new VoucherStatisticsModel
                {
                    Currency = currencyGroup.Key.Currency,
                    Date = currencyGroup.Key.Date,
                };

                foreach (var operationsStatistic in currencyGroup)
                {
                    switch (operationsStatistic.OperationType)
                    {
                        case VoucherOperationType.Redeem:
                            used.Sum += operationsStatistic.Sum;
                            used.Count += operationsStatistic.TotalCount;
                            break;
                        case VoucherOperationType.Buy:
                            bought.Sum += operationsStatistic.Sum;
                            bought.Count += operationsStatistic.TotalCount;
                            break;
                    }
                }

                result.BoughtVoucherStatistics.Add(bought);
                result.UsedVoucherStatistics.Add(used);
            }

            return result;
        }
    }
}
