using AuxiliumSoftware.AuxiliumServices.API.Models.UserStatistic;
using AuxiliumSoftware.AuxiliumServices.Common.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace AuxiliumSoftware.AuxiliumServices.API.Common.Utilities
{
    public static class UserStatisticsCalculationUtilities
    {
        public static async Task<GrowthChartData> GetUserGrowthData(AuxiliumDbContext Db, string period, DateTime now)
        {
            var labels = new List<string>();
            var values = new List<int>();

            switch (period.ToLower())
            {
                case "month":
                    // last 4 weeks
                    for (int i = 3; i >= 0; i--)
                    {
                        var weekStart = now.AddDays(-7 * (i + 1)).Date;
                        var weekEnd = now.AddDays(-7 * i).Date;

                        var count = await Db.Users
                            .CountAsync(u => u.CreatedAt >= weekStart && u.CreatedAt < weekEnd);

                        labels.Add($"Week {4 - i}");
                        values.Add(count);
                    }
                    break;

                case "year":
                    // last 12 months
                    for (int i = 11; i >= 0; i--)
                    {
                        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                            .AddMonths(-i);
                        var monthEnd = monthStart.AddMonths(1);

                        var count = await Db.Users
                            .CountAsync(u => u.CreatedAt >= monthStart && u.CreatedAt < monthEnd);

                        labels.Add(monthStart.ToString("MMM"));
                        values.Add(count);
                    }
                    break;

                case "all":
                    // grab the oldest user to figure out the start date
                    var oldestUser = await Db.Users
                        .OrderBy(u => u.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (oldestUser == null)
                    {
                        return new GrowthChartData
                        {
                            Labels = new List<string>(),
                            Values = new List<int>(),
                            CumulativeValues = new List<int>()
                        };
                    }

                    var startDate = new DateTime(
                        oldestUser.CreatedAt.Year,
                        oldestUser.CreatedAt.Month,
                        1, 0, 0, 0, DateTimeKind.Utc
                    );

                    var totalMonths = ((now.Year - startDate.Year) * 12) + (now.Month - startDate.Month) + 1;

                    if (totalMonths <= 24)
                    {
                        // less than 2 years => show monthly
                        for (var date = startDate; date <= now; date = date.AddMonths(1))
                        {
                            var monthEnd = date.AddMonths(1);

                            var count = await Db.Users
                                .CountAsync(u => u.CreatedAt >= date && u.CreatedAt < monthEnd);

                            labels.Add(date.ToString("MMM yy"));
                            values.Add(count);
                        }
                    }
                    else if (totalMonths <= 60)
                    {
                        // 2-5 years => show quarterly
                        var quarterStart = new DateTime(
                            startDate.Year,
                            ((startDate.Month - 1) / 3) * 3 + 1,
                            1, 0, 0, 0, DateTimeKind.Utc
                        );

                        for (var date = quarterStart; date <= now; date = date.AddMonths(3))
                        {
                            var quarterEnd = date.AddMonths(3);

                            var count = await Db.Users
                                .CountAsync(u => u.CreatedAt >= date && u.CreatedAt < quarterEnd);

                            var quarterNum = ((date.Month - 1) / 3) + 1;
                            labels.Add($"Q{quarterNum} {date:yy}");
                            values.Add(count);
                        }
                    }
                    else
                    {
                        // more than 5 years => show yearly
                        for (int year = startDate.Year; year <= now.Year; year++)
                        {
                            var yearStart = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                            var yearEnd = yearStart.AddYears(1);

                            var count = await Db.Users
                                .CountAsync(u => u.CreatedAt >= yearStart && u.CreatedAt < yearEnd);

                            labels.Add(year.ToString());
                            values.Add(count);
                        }
                    }
                    break;

                default: // the default: weekly
                    // last 7 days
                    for (int i = 6; i >= 0; i--)
                    {
                        var dayStart = now.AddDays(-i).Date;
                        var dayEnd = dayStart.AddDays(1);

                        var count = await Db.Users
                            .CountAsync(u => u.CreatedAt >= dayStart && u.CreatedAt < dayEnd);

                        labels.Add(dayStart.ToString("ddd"));
                        values.Add(count);
                    }
                    break;
            }

            // calculate totals
            var periodStart = period.ToLower() switch
            {
                "month" => now.AddDays(-28).Date,
                "year" => now.AddMonths(-12).Date,
                "all" => DateTime.MinValue,
                _ => now.AddDays(-7).Date
            };

            var cumulativeValues = new List<int>();
            var runningTotal = await Db.Users
                .CountAsync(u => u.CreatedAt < periodStart);

            foreach (var value in values)
            {
                runningTotal += value;
                cumulativeValues.Add(runningTotal);
            }

            return new GrowthChartData
            {
                Labels = labels,
                Values = values,
                CumulativeValues = cumulativeValues
            };
        }

        public static async Task<List<LanguageDistributionItem>> GetLanguageDistributionData(AuxiliumDbContext Db)
        {
            var languageDistribution = await Db.Users
                .Where(u => u.LanguagePreference != null)
                .GroupBy(u => u.LanguagePreference)
                .Select(g => new LanguageDistributionItem
                {
                    Language = g.Key ?? "Unknown",
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            // handle users with no language preference (which shouldn't happen, but edge case yanno lol)
            var noLanguageCount = await Db.Users
                .CountAsync(u => u.LanguagePreference == null);

            if (noLanguageCount > 0)
            {
                languageDistribution.Add(new LanguageDistributionItem
                {
                    Language = "Not Specified",
                    Count = noLanguageCount
                });
            }

            return languageDistribution;
        }
    }
}
