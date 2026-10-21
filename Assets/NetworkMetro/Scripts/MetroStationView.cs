using UnityEngine;
using UnityEngine.UI;

namespace NetworkMetro
{
    // M0 is a manual preview. Event playback will supply states in a later milestone.
    public enum StationState
    {
        NotStarted,
        Running,
        Completed,
        Failed,
        Waiting,
        Skipped
    }

    // Display state of a connection; it does not change either station's state.
    public enum ConnectionState
    {
        Idle,
        Waiting,
        Running,
        Completed,
        Failed
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class MetroStationView : MonoBehaviour
    {
        [SerializeField] private string stationLabel = "Station";
        [SerializeField, TextArea(2, 4)] private string description = "Simulated step.";
        [SerializeField] private StationState state = StationState.NotStarted;

        [Header("Scene references")]
        [SerializeField] private Image lamp;
        [SerializeField] private Text labelText;
        [SerializeField] private Text stateText;
        [SerializeField] private Text descriptionText;

        private bool refreshNeeded = true;
        private bool hasPublishedState;
        private StationState publishedState;

        public StationState State => state;
        public event System.Action<StationState> OnStateChanged;

        // Equality alone does not mean that either station has completed its work.
        public bool IsStateEqualTo(MetroStationView other)
        {
            if (other == null) return false;
            return state == other.state;
        }

        public ConnectionState CompareStatusTo(MetroStationView other)
        {
            if (other == null) return ConnectionState.Idle;
            return CompareStates(state, other.state);
        }

        internal static ConnectionState CompareStates(StationState from, StationState to)
        {
            // A -> B describes arrival at B, after A has completed.
            // If B is still running/waiting/failed, its outgoing B -> C remains idle.
            if (from != StationState.Completed)
                return ConnectionState.Idle;

            switch (to)
            {
                case StationState.Waiting:
                    return ConnectionState.Waiting;
                case StationState.Running:
                    return ConnectionState.Running;
                case StationState.Completed:
                    return ConnectionState.Completed;
                case StationState.Failed:
                    return ConnectionState.Failed;
                default:
                    return ConnectionState.Idle;
            }
        }

        public void Configure(string label, string detail, Image lampImage,
            Text title, Text status, Text body)
        {
            stationLabel = label;
            description = detail;
            lamp = lampImage;
            labelText = title;
            stateText = status;
            descriptionText = body;
            Refresh();
        }

        private void OnEnable() => refreshNeeded = true;

        // OnValidate can run during import; only update UI on the next main-thread update.
        private void OnValidate() => refreshNeeded = true;

        private void Update()
        {
            if (refreshNeeded)
                Refresh();
        }

        public void Refresh()
        {
            refreshNeeded = false;
            Color color;
            string status;
            switch (state)
            {
                case StationState.Running:
                    color = new Color32(255, 200, 76, 255);
                    status = "RUNNING";
                    break;
                case StationState.Completed:
                    color = new Color32(93, 222, 166, 255);
                    status = "COMPLETED";
                    break;
                case StationState.Failed:
                    color = new Color32(255, 113, 126, 255);
                    status = "FAILED / REJECTED";
                    break;
                case StationState.Waiting:
                    color = new Color32(232, 218, 169, 255);
                    status = "WAITING";
                    break;
                case StationState.Skipped:
                    color = new Color32(155, 158, 176, 255);
                    status = "SKIPPED /";
                    break;
                default:
                    color = new Color32(106, 126, 148, 255);
                    status = "NOT STARTED";
                    break;
            }

            if (lamp != null) lamp.color = color;
            if (labelText != null) labelText.text = stationLabel;
            if (descriptionText != null) descriptionText.text = description;
            if (stateText != null)
            {
                stateText.text = status;
                stateText.color = color;
            }

            // Inspector changes reach Refresh through OnValidate -> Update.
            // Notify connections only when the state changes, not when text is edited.
            if (!hasPublishedState || publishedState != state)
            {
                publishedState = state;
                hasPublishedState = true;
                OnStateChanged?.Invoke(state);
            }
        }
    }
}
