namespace CloudHospital.UnreadMessageReminderJob.Models;

public class NotificationModel
{
    public string Id { get; set; }

    public NotificationCode NotificationCode { get; set; }

    public string NotificationTargetId { get; set; }

    public string? SenderId { get; set; }

    public UserModel Sender { get; set; }

    public string? ReceiverId { get; set; }

    public UserModel Receiver { get; set; }

    public string Message { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsChecked { get; set; }

    public bool IsDeleted { get; set; }
}

public enum NotificationCode
{
    // General
    WelcomeEmail = 1000, // Welcome to CloudHospital!

    // Chat
    DirectMessageSent = 2000, // "Username" sent you a message. 

    // Booking
    BookingNew = 3000,
    BookingUpdated = 3050,
    BookingRejected = 3100,
    BookingApproved = 3200,
    BookingPaid = 3300,
    BookingCanceled = 3400,
    BookingRefundRequested = 3500,
    BookingRefunded = 3600,

    /// <summary>
    /// To consultation requested user
    /// Data = { id, message, code, status, consultationType, notificationTargetId = consultationId, confirmedDateStart, hospitalId, hospitalName, doctorId, doctorName, fee}
    /// </summary>
    // Consultation
    ConsultationNew = 4000,
    ConsultationUpdated = 4050,
    ConsultationRejected = 4100,
    ConsultationApproved = 4200,
    ConsultationPaid = 4300,
    ConsultationCanceled = 4400,
    ConsultationRefundRequested = 4500,
    ConsultationRefunded = 4600,
    ConsultationReady = 5000,

    //// Payment
    ///// <summary>
    ///// To Managers
    ///// Data = { code, message, NotificationTargetId = bookingId, userId, userName, userPhoto }
    ///// </summary>
    //UserRefundRequestd = 4000, // "Username" requested a refund for "Booking Title".

    ///// <summary>
    ///// To admins
    ///// Data = { code, message, NotificationTargetId = bookingId, userId, userName, userPhoto }
    ///// </summary>
    //UserRefundRequestCanceled = 4100, // "Username" canceled refund request for "Booking Title".

    ///// <summary>
    ///// To user
    ///// Data = { code, message, NotificationTargetId = bookingId, userId, userName, userPhoto }
    ///// </summary>
    //RefundIssued = 4200, // "Username" in "Hospital name" issued a refund for "Booking Title".        

    ///// <summary>
    ///// Data = { code, message, NotificationTargetId = bookingId, userId, userName, userPhoto }
    ///// </summary>
    //UserPaidForBooking = 4300, // "Username" has paid for "Booking Title"

    ///// <summary>
    ///// To all booking customers
    ///// Data = { code, message, NotificationTargetId = bookingId, userId, userName, userPhoto }
    ///// </summary>
    //BookingCanceled = 4400, // "Booking Title" has been canceled.        
}
