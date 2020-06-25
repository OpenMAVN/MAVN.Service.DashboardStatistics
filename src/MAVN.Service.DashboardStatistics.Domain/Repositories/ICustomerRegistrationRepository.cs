﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MAVN.Service.DashboardStatistics.Domain.Repositories
{
    public interface ICustomerRegistrationRepository
    {
        Task<int> GetCountSync(DateTime endDate, Guid[] partnerIds, bool filterByPartnerIds);

        Task<IReadOnlyDictionary<DateTime, int>> GetCountPerDayAsync(DateTime startDate, DateTime endDate, Guid[] partnerIds, bool filterByPartnerIds);

        Task InsertIfNotExistsAsync(Guid customerId, Guid? partnerId, DateTime registrationDate);
    }
}
