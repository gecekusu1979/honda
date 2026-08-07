using System;

namespace HondaTuner.Core.AutoTune
{
    public class TuneExplanationProvider : ITuneExplanationProvider
    {
        public string GenerateExplanation(TuneDecision decision, string userRole)
        {
            if (decision == null) return "Geçersiz karar verisi.";

            string approvalReq;
            if (decision.ApprovalStatus == "Approved")
            {
                approvalReq = "Doğrudan onaylandı / Gerekmiyor";
            }
            else if (decision.ApprovalStatus == "PendingApproval")
            {
                approvalReq = "Advanced onayı gerekiyor";
            }
            else if (decision.ApprovalStatus == "Rejected")
            {
                approvalReq = "Beginner yetkisi/Kullanıcı engellemesi";
            }
            else
            {
                approvalReq = "Onay bekleniyor";
            }

            string safetyText = "FAILED";
            if (decision.Safety != null)
            {
                safetyText = decision.Safety.Status == "Allow" ? "PASSED" : $"FAILED ({decision.Safety.Reason})";
            }

            return $"Parameter changed: {decision.ParameterName}\r\n" +
                   $"Old Value: {decision.OldValue:F2}\r\n" +
                   $"New Value: {decision.NewValue:F2}\r\n" +
                   $"Reason: {decision.ConfidenceReason}\r\n" +
                   $"Confidence: {decision.ConfidenceScore:F0}%\r\n" +
                   $"Safety Validation: {safetyText}\r\n" +
                   $"User approval requirement: {approvalReq}";
        }
    }
}
