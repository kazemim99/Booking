// src/modules/provider/services/notification-templates.service.ts

import type { ProviderInvitation, ProviderJoinRequest } from '../types/hierarchy.types'

/**
 * Notification Templates Service
 * Generates SMS and email content for provider hierarchy notifications
 */

interface SmsTemplate {
  content: string
  maxLength: 160 // Standard SMS length
}

interface EmailTemplate {
  subject: string
  body: string
  isHtml?: boolean
}

/**
 * SMS Templates
 */
export const smsTemplates = {
  /**
   * Invitation sent to individual to join organization
   */
  invitationSent: (organizationName: string, invitationId: string, expiresIn: number): SmsTemplate => {
    const acceptUrl = `${window.location.origin}/invitations/${invitationId}/accept`
    return {
      content: `${organizationName} شما را به عنوان کارمند دعوت کرده است. برای پذیرش دعوت روی لینک کلیک کنید: ${acceptUrl} (اعتبار: ${expiresIn} روز)`,
      maxLength: 160,
    }
  },

  /**
   * Invitation accepted notification to organization
   */
  invitationAccepted: (staffName: string, organizationName: string): SmsTemplate => {
    return {
      content: `${staffName} دعوت شما برای پیوستن به ${organizationName} را پذیرفت و به تیم شما اضافه شد.`,
      maxLength: 160,
    }
  },

  /**
   * Invitation rejected notification to organization
   */
  invitationRejected: (staffName: string, organizationName: string): SmsTemplate => {
    return {
      content: `${staffName} دعوت شما برای پیوستن به ${organizationName} را رد کرد.`,
      maxLength: 160,
    }
  },

  /**
   * Join request received notification to organization
   */
  joinRequestReceived: (requesterName: string, organizationName: string): SmsTemplate => {
    return {
      content: `${requesterName} درخواست پیوستن به ${organizationName} را ارسال کرده است. برای بررسی وارد پنل مدیریت شوید.`,
      maxLength: 160,
    }
  },

  /**
   * Join request approved notification to requester
   */
  joinRequestApproved: (organizationName: string): SmsTemplate => {
    return {
      content: `درخواست پیوستن شما به ${organizationName} تایید شد! شما اکنون عضو این مجموعه هستید.`,
      maxLength: 160,
    }
  },

  /**
   * Join request rejected notification to requester
   */
  joinRequestRejected: (organizationName: string, reason?: string): SmsTemplate => {
    const baseMessage = `درخواست پیوستن شما به ${organizationName} رد شد.`
    const fullMessage = reason ? `${baseMessage} دلیل: ${reason}` : baseMessage
    return {
      content: fullMessage,
      maxLength: 160,
    }
  },

  /**
   * Invitation expiry reminder (24 hours before expiry)
   */
  invitationExpiryReminder: (organizationName: string, invitationId: string): SmsTemplate => {
    const acceptUrl = `${window.location.origin}/invitations/${invitationId}/accept`
    return {
      content: `یادآوری: دعوت ${organizationName} تا 24 ساعت دیگر منقضی می‌شود. برای پذیرش: ${acceptUrl}`,
      maxLength: 160,
    }
  },

  /**
   * Staff member removed notification
   */
  staffMemberRemoved: (organizationName: string, reason: string): SmsTemplate => {
    return {
      content: `شما از تیم ${organizationName} حذف شدید. دلیل: ${reason}`,
      maxLength: 160,
    }
  },
}

/**
 * Email Templates
 */
export const emailTemplates = {
  /**
   * Invitation sent email (HTML)
   */
  invitationSent: (
    invitation: ProviderInvitation,
    organizationLogo?: string,
  ): EmailTemplate => {
    const acceptUrl = `${window.location.origin}/invitations/${invitation.id}/accept?org=${invitation.organizationId}`
    const expiryDate = new Date(invitation.expiresAt).toLocaleDateString('fa-IR')

    return {
      subject: `دعوت به پیوستن به ${invitation.organizationName}`,
      body: `
<!DOCTYPE html>
<html dir="rtl" lang="fa">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <style>
    body { font-family: 'Tahoma', 'Arial', sans-serif; background: #f7f9fc; margin: 0; padding: 20px; }
    .container { max-width: 600px; margin: 0 auto; background: white; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 16px rgba(0,0,0,0.1); }
    .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px 20px; text-align: center; }
    .logo { width: 80px; height: 80px; background: white; border-radius: 50%; margin: 0 auto 20px; display: flex; align-items: center; justify-content: center; }
    .header h1 { color: white; margin: 0; font-size: 24px; }
    .content { padding: 40px 30px; }
    .content h2 { color: #1e293b; font-size: 20px; margin-bottom: 20px; }
    .content p { color: #475569; line-height: 1.8; margin-bottom: 20px; }
    .message-box { background: #f8fafc; border-right: 4px solid #667eea; padding: 20px; margin: 20px 0; border-radius: 8px; }
    .cta-button { display: inline-block; padding: 16px 32px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; text-decoration: none; border-radius: 12px; font-weight: bold; margin: 20px 0; }
    .cta-button:hover { opacity: 0.9; }
    .info-box { background: #fef9e7; border: 1px solid #f39c12; border-radius: 8px; padding: 16px; margin: 20px 0; }
    .footer { background: #f8fafc; padding: 30px; text-align: center; color: #64748b; font-size: 14px; border-top: 1px solid #e2e8f0; }
  </style>
</head>
<body>
  <div class="container">
    <div class="header">
      ${organizationLogo ? `<div class="logo"><img src="${organizationLogo}" alt="Logo" style="width: 60px; height: 60px; border-radius: 50%;"></div>` : ''}
      <h1>دعوت به پیوستن به تیم</h1>
    </div>

    <div class="content">
      <h2>سلام ${invitation.inviteeName || 'کاربر گرامی'}،</h2>

      <p>
        شما از طرف <strong>${invitation.organizationName}</strong> برای پیوستن به تیم آنها دعوت شده‌اید.
      </p>

      ${invitation.message ? `
      <div class="message-box">
        <strong>پیام سازمان:</strong><br>
        ${invitation.message}
      </div>
      ` : ''}

      <p>
        با پذیرش این دعوت، شما به عنوان یک متخصص در این مجموعه ثبت خواهید شد و می‌توانید خدمات خود را ارائه دهید.
      </p>

      <div style="text-align: center;">
        <a href="${acceptUrl}" class="cta-button">پذیرش دعوت</a>
      </div>

      <div class="info-box">
        <strong>⏰ توجه:</strong> این دعوت تا تاریخ <strong>${expiryDate}</strong> معتبر است.
      </div>

      <p style="font-size: 14px; color: #64748b;">
        اگر شما این درخواست را ارسال نکرده‌اید، لطفاً این ایمیل را نادیده بگیرید.
      </p>
    </div>

    <div class="footer">
      <p>این ایمیل توسط سیستم Booksy ارسال شده است.</p>
      <p>لطفاً به این ایمیل پاسخ ندهید.</p>
    </div>
  </div>
</body>
</html>
      `,
      isHtml: true,
    }
  },

  /**
   * Invitation accepted email to organization
   */
  invitationAccepted: (staffName: string, organizationName: string): EmailTemplate => {
    return {
      subject: `${staffName} به تیم شما پیوست`,
      body: `
<!DOCTYPE html>
<html dir="rtl" lang="fa">
<head>
  <meta charset="UTF-8">
  <style>
    body { font-family: 'Tahoma', 'Arial', sans-serif; background: #f7f9fc; margin: 0; padding: 20px; }
    .container { max-width: 600px; margin: 0 auto; background: white; border-radius: 16px; padding: 40px; box-shadow: 0 4px 16px rgba(0,0,0,0.1); }
    .success-icon { width: 80px; height: 80px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); border-radius: 50%; margin: 0 auto 20px; display: flex; align-items: center; justify-content: center; font-size: 40px; color: white; }
    h1 { color: #1e293b; text-align: center; margin-bottom: 20px; }
    p { color: #475569; line-height: 1.8; }
    .cta-button { display: inline-block; padding: 16px 32px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; text-decoration: none; border-radius: 12px; font-weight: bold; margin: 20px 0; }
  </style>
</head>
<body>
  <div class="container">
    <div class="success-icon">✓</div>
    <h1>عضو جدید به تیم شما اضافه شد!</h1>
    <p><strong>${staffName}</strong> دعوت شما را پذیرفت و اکنون عضو تیم <strong>${organizationName}</strong> است.</p>
    <p>شما می‌توانید از طریق پنل مدیریت کارمندان، اطلاعات و دسترسی‌های این عضو را مدیریت کنید.</p>
    <div style="text-align: center;">
      <a href="${window.location.origin}/staff" class="cta-button">مدیریت کارمندان</a>
    </div>
  </div>
</body>
</html>
      `,
      isHtml: true,
    }
  },

  /**
   * Join request received email to organization
   */
  joinRequestReceived: (request: ProviderJoinRequest): EmailTemplate => {
    const reviewUrl = `${window.location.origin}/staff`

    return {
      subject: `درخواست جدید برای پیوستن به ${request.organizationName}`,
      body: `
<!DOCTYPE html>
<html dir="rtl" lang="fa">
<head>
  <meta charset="UTF-8">
  <style>
    body { font-family: 'Tahoma', 'Arial', sans-serif; background: #f7f9fc; margin: 0; padding: 20px; }
    .container { max-width: 600px; margin: 0 auto; background: white; border-radius: 16px; padding: 40px; box-shadow: 0 4px 16px rgba(0,0,0,0.1); }
    .notification-icon { width: 80px; height: 80px; background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); border-radius: 50%; margin: 0 auto 20px; display: flex; align-items: center; justify-content: center; font-size: 40px; color: white; }
    h1 { color: #1e293b; text-align: center; margin-bottom: 20px; }
    p { color: #475569; line-height: 1.8; }
    .requester-info { background: #f8fafc; padding: 20px; border-radius: 12px; margin: 20px 0; }
    .message-box { background: #fef9e7; border-right: 4px solid #f39c12; padding: 16px; margin: 20px 0; border-radius: 8px; }
    .cta-button { display: inline-block; padding: 16px 32px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; text-decoration: none; border-radius: 12px; font-weight: bold; margin: 20px 0; }
  </style>
</head>
<body>
  <div class="container">
    <div class="notification-icon">📩</div>
    <h1>درخواست جدید برای پیوستن</h1>

    <p><strong>${request.requesterName}</strong> درخواست پیوستن به <strong>${request.organizationName}</strong> را ارسال کرده است.</p>

    <div class="requester-info">
      <strong>نام متقاضی:</strong> ${request.requesterName}<br>
      <strong>تاریخ درخواست:</strong> ${new Date(request.createdAt).toLocaleDateString('fa-IR')}
    </div>

    ${request.message ? `
    <div class="message-box">
      <strong>پیام متقاضی:</strong><br>
      ${request.message}
    </div>
    ` : ''}

    <p>لطفاً درخواست را بررسی کرده و تصمیم خود را اعلام کنید.</p>

    <div style="text-align: center;">
      <a href="${reviewUrl}" class="cta-button">بررسی درخواست</a>
    </div>
  </div>
</body>
</html>
      `,
      isHtml: true,
    }
  },

  /**
   * Join request approved email to requester
   */
  joinRequestApproved: (organizationName: string): EmailTemplate => {
    return {
      subject: `درخواست شما به ${organizationName} تایید شد`,
      body: `
<!DOCTYPE html>
<html dir="rtl" lang="fa">
<head>
  <meta charset="UTF-8">
  <style>
    body { font-family: 'Tahoma', 'Arial', sans-serif; background: #f7f9fc; margin: 0; padding: 20px; }
    .container { max-width: 600px; margin: 0 auto; background: white; border-radius: 16px; padding: 40px; box-shadow: 0 4px 16px rgba(0,0,0,0.1); }
    .success-icon { width: 80px; height: 80px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); border-radius: 50%; margin: 0 auto 20px; display: flex; align-items: center; justify-content: center; font-size: 40px; color: white; }
    h1 { color: #1e293b; text-align: center; margin-bottom: 20px; }
    p { color: #475569; line-height: 1.8; }
    .cta-button { display: inline-block; padding: 16px 32px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; text-decoration: none; border-radius: 12px; font-weight: bold; margin: 20px 0; }
  </style>
</head>
<body>
  <div class="container">
    <div class="success-icon">🎉</div>
    <h1>تبریک! درخواست شما تایید شد</h1>
    <p>درخواست پیوستن شما به <strong>${organizationName}</strong> تایید شد.</p>
    <p>شما اکنون می‌توانید به عنوان یک عضو تیم، خدمات خود را ارائه دهید و رزروها را مدیریت کنید.</p>
    <div style="text-align: center;">
      <a href="${window.location.origin}/dashboard" class="cta-button">رفتن به داشبورد</a>
    </div>
  </div>
</body>
</html>
      `,
      isHtml: true,
    }
  },

  /**
   * Join request rejected email to requester
   */
  joinRequestRejected: (organizationName: string, reason?: string): EmailTemplate => {
    return {
      subject: `درخواست پیوستن به ${organizationName}`,
      body: `
<!DOCTYPE html>
<html dir="rtl" lang="fa">
<head>
  <meta charset="UTF-8">
  <style>
    body { font-family: 'Tahoma', 'Arial', sans-serif; background: #f7f9fc; margin: 0; padding: 20px; }
    .container { max-width: 600px; margin: 0 auto; background: white; border-radius: 16px; padding: 40px; box-shadow: 0 4px 16px rgba(0,0,0,0.1); }
    .info-icon { width: 80px; height: 80px; background: linear-gradient(135deg, #64748b 0%, #475569 100%); border-radius: 50%; margin: 0 auto 20px; display: flex; align-items: center; justify-content: center; font-size: 40px; color: white; }
    h1 { color: #1e293b; text-align: center; margin-bottom: 20px; }
    p { color: #475569; line-height: 1.8; }
    .reason-box { background: #fef2f2; border-right: 4px solid #ef4444; padding: 16px; margin: 20px 0; border-radius: 8px; }
  </style>
</head>
<body>
  <div class="container">
    <div class="info-icon">ℹ️</div>
    <h1>درباره درخواست شما</h1>
    <p>متاسفانه درخواست پیوستن شما به <strong>${organizationName}</strong> در این زمان تایید نشد.</p>
    ${reason ? `
    <div class="reason-box">
      <strong>دلیل:</strong><br>
      ${reason}
    </div>
    ` : ''}
    <p>شما می‌توانید به جستجوی سازمان‌های دیگر ادامه دهید یا در آینده مجدداً درخواست ارسال کنید.</p>
  </div>
</body>
</html>
      `,
      isHtml: true,
    }
  },
}

/**
 * Notification Service
 * Sends notifications via SMS and Email
 */
export class NotificationTemplatesService {
  /**
   * Generate SMS content for invitation
   */
  static generateInvitationSms(
    organizationName: string,
    invitationId: string,
    expiresInDays: number = 7,
  ): string {
    return smsTemplates.invitationSent(organizationName, invitationId, expiresInDays).content
  }

  /**
   * Generate email HTML for invitation
   */
  static generateInvitationEmail(
    invitation: ProviderInvitation,
    organizationLogo?: string,
  ): EmailTemplate {
    return emailTemplates.invitationSent(invitation, organizationLogo)
  }

  /**
   * Generate join request received notification
   */
  static generateJoinRequestReceivedEmail(request: ProviderJoinRequest): EmailTemplate {
    return emailTemplates.joinRequestReceived(request)
  }

  /**
   * Generate join request approved notification
   */
  static generateJoinRequestApprovedEmail(organizationName: string): EmailTemplate {
    return emailTemplates.joinRequestApproved(organizationName)
  }

  /**
   * Calculate days until expiry
   */
  static getDaysUntilExpiry(expiresAt: string): number {
    const expiry = new Date(expiresAt)
    const now = new Date()
    const diffTime = expiry.getTime() - now.getTime()
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24))
  }
}

export default NotificationTemplatesService
