using System.Globalization;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.WithinTimeWindow;

/// <summary>
/// Pure calendar logic for checking if a UTC instant falls within a configured time window.
/// </summary>
internal static class TimeWindowCalculator
{
    /// <summary>
    /// Determines if the given UTC instant is within the configured time window.
    /// </summary>
    /// <param name="utcInstant">The UTC instant to check.</param>
    /// <param name="selectedWeekdays">Weekday names (e.g., "Monday", "Tuesday").</param>
    /// <param name="startTime">Start time in HH:mm format.</param>
    /// <param name="endTime">End time in HH:mm format.</param>
    /// <param name="timeZoneId">Time zone ID from TimeZoneInfo.GetSystemTimeZones().</param>
    /// <param name="startDate">Optional inclusive start date in yyyy-MM-dd format.</param>
    /// <param name="endDate">Optional inclusive end date in yyyy-MM-dd format.</param>
    /// <returns>True if the instant is within the window; false otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown on configuration validation failure.</exception>
    public static bool IsWithinWindow(
        DateTime utcInstant,
        IList<string> selectedWeekdays,
        string startTime,
        string endTime,
        string timeZoneId,
        string startDate,
        string endDate)
    {
        // Validate configuration independently of time
        ValidateConfiguration(selectedWeekdays, startTime, endTime, timeZoneId, startDate, endDate);

        // Get local time
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var localDateTime = TimeZoneInfo.ConvertTime(utcInstant, timeZone);
        var localTime = localDateTime.TimeOfDay;
        var localDate = DateOnly.FromDateTime(localDateTime);

        // Parse times
        var startTimeOnly = TimeOnly.Parse(startTime, CultureInfo.InvariantCulture);
        var endTimeOnly = TimeOnly.Parse(endTime, CultureInfo.InvariantCulture);
        var startTimeSpan = startTimeOnly.ToTimeSpan();
        var endTimeSpan = endTimeOnly.ToTimeSpan();

        // Determine if same-day or overnight
        bool isSameDay = startTimeSpan <= endTimeSpan;

        // Calculate the window start date
        var windowStartDate = isSameDay
            ? DetermineWindowStartDateSameDay(localTime, startTimeSpan, endTimeSpan, localDate)
            : DetermineWindowStartDateOvernight(localTime, startTimeSpan, localDate);

        // Check if the window start date's weekday is selected
        var weekday = windowStartDate.DayOfWeek;
        if (!selectedWeekdays.Any(w => string.Equals(w, weekday.ToString(), StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        // Check if window start date falls within optional date limits
        if ((!string.IsNullOrWhiteSpace(startDate) || !string.IsNullOrWhiteSpace(endDate))
            && !IsDateWithinLimits(windowStartDate, startDate, endDate))
        {
            return false;
        }

        return true;
    }


    private static void ValidateConfiguration(
        IList<string> selectedWeekdays,
        string startTime,
        string endTime,
        string timeZoneId,
        string startDate,
        string endDate)
    {
        if (selectedWeekdays == null || selectedWeekdays.Count == 0)
        {
            throw new ArgumentException("At least one weekday must be selected.", nameof(selectedWeekdays));
        }

        // Validate weekday names
        var validDays = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
        };

        var invalidDays = selectedWeekdays.Where(day => !validDays.Contains(day)).ToList();
        if (invalidDays.Any())
        {
            throw new ArgumentException($"Invalid weekday: {string.Join(", ", invalidDays)}", nameof(selectedWeekdays));
        }

        // Validate times
        if (!TimeOnly.TryParse(startTime, CultureInfo.InvariantCulture, out _))
        {
            throw new ArgumentException($"Invalid start time format: {startTime}. Use HH:mm format.", nameof(startTime));
        }

        if (!TimeOnly.TryParse(endTime, CultureInfo.InvariantCulture, out _))
        {
            throw new ArgumentException($"Invalid end time format: {endTime}. Use HH:mm format.", nameof(endTime));
        }

        // Validate time zone
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ArgumentException($"Time zone not found: {timeZoneId}", nameof(timeZoneId));
        }

        // Validate dates
        if (!string.IsNullOrWhiteSpace(startDate)
            && !DateOnly.TryParse(startDate, CultureInfo.InvariantCulture, out _))
        {
            throw new ArgumentException($"Invalid start date format: {startDate}. Use yyyy-MM-dd format.", nameof(startDate));
        }

        if (!string.IsNullOrWhiteSpace(endDate)
            && !DateOnly.TryParse(endDate, CultureInfo.InvariantCulture, out _))
        {
            throw new ArgumentException($"Invalid end date format: {endDate}. Use yyyy-MM-dd format.", nameof(endDate));
        }

        // Validate date order
        if (!string.IsNullOrWhiteSpace(startDate) && !string.IsNullOrWhiteSpace(endDate))
        {
            var start = DateOnly.Parse(startDate, CultureInfo.InvariantCulture);
            var end = DateOnly.Parse(endDate, CultureInfo.InvariantCulture);
            if (start > end)
            {
                throw new ArgumentException($"Start date {startDate} cannot be after end date {endDate}", nameof(startDate));
            }
        }
    }


    private static DateOnly DetermineWindowStartDateSameDay(TimeSpan localTime, TimeSpan startTimeSpan, TimeSpan endTimeSpan, DateOnly currentLocalDate)
    {
        // Same-day window: current local date if start <= local time < end
        if (localTime >= startTimeSpan && localTime < endTimeSpan)
        {
            return currentLocalDate;
        }

        return currentLocalDate.AddDays(-1); // Outside window
    }


    private static DateOnly DetermineWindowStartDateOvernight(TimeSpan localTime, TimeSpan startTimeSpan, DateOnly currentLocalDate)
    {
        // Overnight window: current local date if local time >= start; previous local date if local time < end
        if (localTime >= startTimeSpan)
        {
            return currentLocalDate; // After start, so window started today
        }

        return currentLocalDate.AddDays(-1); // Before start, so window started yesterday
    }


    private static bool IsDateWithinLimits(DateOnly date, string startDate, string endDate)
    {
        DateOnly? parsedStartDate = null;
        DateOnly? parsedEndDate = null;

        if (!string.IsNullOrWhiteSpace(startDate))
        {
            parsedStartDate = DateOnly.Parse(startDate, CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(endDate))
        {
            parsedEndDate = DateOnly.Parse(endDate, CultureInfo.InvariantCulture);
        }

        if (parsedStartDate.HasValue && date < parsedStartDate.Value)
        {
            return false;
        }

        if (parsedEndDate.HasValue && date > parsedEndDate.Value)
        {
            return false;
        }

        return true;
    }
}

