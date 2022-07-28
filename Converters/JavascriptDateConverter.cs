using System.Text.Json;
using System.Text.Json.Serialization;


namespace CloudHospital.UnreadMessageReminderJob.Converters;

public sealed class DateTimeJsonCoonverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        DateTime value;
        if (reader.TokenType == JsonTokenType.Number)
        {
            double doubleValue;
            if (reader.TryGetDouble(out doubleValue))
            {
                var converter = new JavascriptDateConverter();
                var datetimeOffsetValue = converter.ToDateTimeOffset(doubleValue);

                if (datetimeOffsetValue.HasValue)
                {
                    return datetimeOffsetValue.Value.UtcDateTime;
                }
            }
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();

            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return null;
            }

            if (reader.TryGetDateTime(out value))
            {
                return value;
            }

            if (DateTime.TryParse(reader.GetString()!, out value))
            {
                return value;
            }
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {

            writer.WriteStringValue($"{value.Value:yyyy-MM-ddTHH:mm:ssZ}");
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

/// <summary>
/// Javascript Date milliseconds value converter
/// </summary>
public sealed class JavascriptDateConverter
{
    /// <summary>
    /// Javscript Date milliseconds value to <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="javascriptDateMillisecondsValue"></param>
    /// <returns></returns>
    public DateTimeOffset? ToDateTimeOffset(double? javascriptDateMillisecondsValue)
    {
        if (!javascriptDateMillisecondsValue.HasValue)
        {
            return null;
        }

        return JAVASCRIPT_DATE_BASIS.AddMilliseconds(javascriptDateMillisecondsValue.Value);
    }

    /// <summary>
    /// <see cref="DateTimeOffset"/> to Javascript Date milliseconds value
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public double? ToJavascriptDateMilliseconds(DateTimeOffset? value)
    {
        if (!value.HasValue) { return null; }

        var sourceValue = value.Value;
        return sourceValue.Subtract(JAVASCRIPT_DATE_BASIS).TotalMilliseconds;
    }

    public readonly DateTimeOffset JAVASCRIPT_DATE_BASIS = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
}