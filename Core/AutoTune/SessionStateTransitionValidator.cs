using System;

namespace HondaTuner.Core.AutoTune
{
    public static class SessionStateTransitionValidator
    {
        public static bool CanTransition(SessionState current, SessionState target, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (current == target) return true;

            switch (current)
            {
                case SessionState.Initializing:
                    if (target == SessionState.Running || target == SessionState.Error)
                        return true;
                    break;
                case SessionState.Running:
                    if (target == SessionState.Paused || target == SessionState.Stopped || target == SessionState.Error)
                        return true;
                    break;
                case SessionState.Paused:
                    if (target == SessionState.Running || target == SessionState.Stopped)
                        return true;
                    break;
                case SessionState.Stopped:
                case SessionState.Error:
                    // Terminal states
                    errorMessage = $"Cannot transition from terminal state: {current}";
                    return false;
            }

            errorMessage = $"Invalid transition: {current} -> {target}";
            return false;
        }

        public static SessionState RequireTransition(SessionState current, SessionState target)
        {
            if (!CanTransition(current, target, out string err))
                throw new InvalidOperationException($"SessionState transition BLOCKED: {err}");

            return target;
        }
    }
}
