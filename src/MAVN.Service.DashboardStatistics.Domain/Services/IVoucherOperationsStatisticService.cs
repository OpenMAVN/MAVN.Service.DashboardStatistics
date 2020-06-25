﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MAVN.Service.DashboardStatistics.Domain.Models.VoucherStatistic;

namespace MAVN.Service.DashboardStatistics.Domain.Services
{
    public interface IVoucherOperationsStatisticService
    {
        Task UpdateVoucherOperationsStatistic(UpdateVoucherOperationsStatistic partnerStatistic);

        Task<IList<CurrenciesStatistic>> GetCurrenciesStatistic(Guid[] partnerIds, bool filterByPartnerIds);

        Task<VouchersDailyStatistics> GetPartnerDailyVoucherStatistic(Guid[] partnerIds, bool filterByPartnerIds, DateTime fromDate,
            DateTime toDate);
    }
}
