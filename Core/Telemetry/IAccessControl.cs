using System;

namespace HondaTuner.Core.Telemetry
{
    /// <summary>
    /// Telemetri ve tuning erişim yetki rolleri.
    /// </summary>
    public enum TelemetryRole
    {
        ReadOnly,
        Calibration,
        Flash,
        Developer
    }

    /// <summary>
    /// Telemetri işlemlerinin yetkilendirilmesini kontrol eden erişim kontrol arayüzüdür.
    /// </summary>
    public interface IAccessControl
    {
        TelemetryRole CurrentRole { get; }

        /// <summary>
        /// Geçerli kullanıcının rolünü değiştirir.
        /// </summary>
        void SetCurrentRole(TelemetryRole role);

        /// <summary>
        /// Belirtilen rol seviyesinde bir işleme erişimi olup olmadığını doğrular.
        /// </summary>
        bool Authorize(TelemetryRole requiredRole, string operation, out string auditMessage);
    }

    public class AccessControl : IAccessControl
    {
        private TelemetryRole _currentRole = TelemetryRole.ReadOnly;
        private readonly object _lock = new object();

        public TelemetryRole CurrentRole
        {
            get { lock (_lock) { return _currentRole; } }
        }

        public void SetCurrentRole(TelemetryRole role)
        {
            lock (_lock)
            {
                _currentRole = role;
            }
        }

        public bool Authorize(TelemetryRole requiredRole, string operation, out string auditMessage)
        {
            lock (_lock)
            {
                // Hiyerarşik yetki kontrolü: Developer > Flash > Calibration > ReadOnly
                bool authorized = _currentRole >= requiredRole;
                if (authorized)
                {
                    auditMessage = $"Erişim Onaylandı: Rol={_currentRole}, Talep={requiredRole}, İşlem={operation}";
                    return true;
                }
                else
                {
                    auditMessage = $"Erişim REDDEDİLDİ: Geçerli Rol={_currentRole}, Gerekli Rol={requiredRole}, İşlem={operation}";
                    return false;
                }
            }
        }
    }
}
