using UnityEngine;
using UnityEngine.UI;

namespace NetworkMetro
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class StationConnectorView : MonoBehaviour
    {
        [SerializeField] private Image trackImage;
        [SerializeField] private MetroStationView fromStation;
        [SerializeField] private MetroStationView toStation;
        [Header("Play Mode timing")]
        [SerializeField, Min(0.25f), Tooltip("Seconds for one complete waiting breath.")]
        private float waitingBreathSeconds = 6f;
        [SerializeField, Min(0.1f), Tooltip("Seconds for the light band to travel from A to B.")]
        private float runningTravelSeconds = 3f;

        private static readonly Color Idle = new Color32(57, 81, 105, 255);
        private static readonly Color Completed = new Color32(93, 222, 166, 255);
        private static readonly Color Waiting = new Color32(255, 200, 76, 255);
        private static readonly Color Running = new Color32(80, 181, 207, 255);
        private static readonly Color Failed = new Color32(255, 113, 126, 255);
        private static readonly Color Packet = new Color32(184, 246, 255, 255);
        private MetroStationView subscribedFrom;
        private MetroStationView subscribedTo;
        private bool refreshNeeded = true;
        private ConnectionState comparison = ConnectionState.Idle;
        private float animTime;
        private Image flowPacket;

        public ConnectionState VisualState => comparison;

        public void Configure(MetroStationView from, MetroStationView to, Image track)
        {
            fromStation = from;
            toStation = to;
            trackImage = track;

            if (isActiveAndEnabled)
                Rebind();
            else
                refreshNeeded = true;
        }

        private void OnEnable() => refreshNeeded = true;

        // OnValidate may run during import. Rebind and update UI on the main thread.
        private void OnValidate() => refreshNeeded = true;

        private void Rebind()
        {
            refreshNeeded = false;
            Unsubscribe();
            ReleasePacket();
            if (trackImage == null)
                trackImage = GetComponent<Image>();

            subscribedFrom = fromStation;
            subscribedTo = toStation;
            if (subscribedFrom != null)
                subscribedFrom.OnStateChanged += HandleStateChange;
            if (subscribedTo != null && subscribedTo != subscribedFrom)
                subscribedTo.OnStateChanged += HandleStateChange;

            RefreshImmediate();
        }

        private void HandleStateChange(StationState _) => RefreshImmediate();

        private void RefreshImmediate()
        {
            ConnectionState next = fromStation != null
                ? fromStation.CompareStatusTo(toStation)
                : ConnectionState.Idle;
            if (next != comparison)
                animTime = 0f;
            comparison = next;

            if (flowPacket != null && comparison != ConnectionState.Running)
                flowPacket.enabled = false;

            if (trackImage == null)
                return;

            switch (comparison)
            {
                case ConnectionState.Completed:
                    trackImage.color = Completed;
                    break;
                case ConnectionState.Waiting:
                    trackImage.color = Waiting;
                    break;
                case ConnectionState.Running:
                    trackImage.color = Running;
                    break;
                case ConnectionState.Failed:
                    trackImage.color = Failed;
                    break;
                default:
                    trackImage.color = Idle;
                    break;
            }
        }

        private void Update()
        {
            if (refreshNeeded)
                Rebind();

            // Edit mode is a still preview. Generated graphics exist only during Play Mode.
            if (!Application.IsPlaying(gameObject) || trackImage == null)
                return;

            if (fromStation == null || toStation == null)
            {
                RefreshImmediate();
                return;
            }

            if (comparison == ConnectionState.Waiting)
            {
                float period = Mathf.Max(0.25f, waitingBreathSeconds);
                animTime = (animTime + Time.unscaledDeltaTime) % period;
                float pulse = 0.5f + 0.5f * Mathf.Cos(animTime / period * Mathf.PI * 2f);
                trackImage.color = Color.Lerp(Idle, Waiting, 0.25f + 0.75f * pulse);
            }
            else if (comparison == ConnectionState.Running)
            {
                float period = Mathf.Max(0.1f, runningTravelSeconds);
                animTime = (animTime + Time.unscaledDeltaTime) % period;
                UpdatePacket(animTime / period);
            }
        }

        private void UpdatePacket(float progress)
        {
            if (flowPacket == null)
            {
                var packetObject = new GameObject("Flow Band (Play Mode)",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                packetObject.hideFlags = HideFlags.DontSave;
                flowPacket = packetObject.GetComponent<Image>();
                flowPacket.raycastTarget = false;
                flowPacket.color = Packet;
                RectTransform rect = flowPacket.rectTransform;
                rect.SetParent(trackImage.rectTransform, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(24f, 10f);
            }

            // Convert the actual endpoints into track-local space so A -> B is explicit,
            // including reversed or diagonal connectors. These examples use straight tracks.
            RectTransform track = trackImage.rectTransform;
            Vector3 start = track.InverseTransformPoint(fromStation.transform.position);
            Vector3 end = track.InverseTransformPoint(toStation.transform.position);
            Vector3 direction = end - start;
            flowPacket.enabled = direction.sqrMagnitude > 0.0001f;
            flowPacket.rectTransform.localPosition = Vector3.Lerp(start, end, progress);
            flowPacket.rectTransform.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        private void OnDisable()
        {
            Unsubscribe();
            ReleasePacket();
        }

        private void ReleasePacket()
        {
            if (flowPacket == null)
                return;

            // Hide immediately; runtime Destroy finishes at the end of the frame.
            flowPacket.enabled = false;
            if (Application.IsPlaying(flowPacket.gameObject))
                Destroy(flowPacket.gameObject);
            else
                DestroyImmediate(flowPacket.gameObject);
            flowPacket = null;
        }

        private void Unsubscribe()
        {
            if (subscribedFrom != null)
                subscribedFrom.OnStateChanged -= HandleStateChange;
            if (subscribedTo != null && subscribedTo != subscribedFrom)
                subscribedTo.OnStateChanged -= HandleStateChange;
            subscribedFrom = null;
            subscribedTo = null;
        }
    }
}
