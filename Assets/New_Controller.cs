using System;
using System.IO;
using UnityEngine;

namespace Oculus.Interaction.Input
{
    public class New_Controller : DataModifier<ControllerDataAsset>, IController
    {
        public virtual Handedness Handedness => GetData().Config.Handedness;

        public virtual bool IsConnected
        {
            get
            {
                var currentData = GetData();
                return currentData.IsDataValid && currentData.IsConnected;
            }
        }

        public virtual bool IsPoseValid
        {
            get
            {
                var currentData = GetData();
                return currentData.IsDataValid &&
                       currentData.RootPoseOrigin != PoseOrigin.None;
            }
        }

        public virtual bool IsPointerPoseValid
        {
            get
            {
                var currentData = GetData();
                return currentData.IsDataValid &&
                       currentData.PointerPoseOrigin != PoseOrigin.None;
            }
        }

        public virtual ControllerInput ControllerInput
        {
            get
            {
                var currentData = GetData();
                return currentData.Input;
            }
        }

        public virtual event Action WhenUpdated = delegate { };

        private ITrackingToWorldTransformer TrackingToWorldTransformer =>
            GetData().Config.TrackingToWorldTransformer;

        public virtual float Scale => TrackingToWorldTransformer != null
            ? TrackingToWorldTransformer.Transform.lossyScale.x
            : 1;

        public virtual bool IsButtonUsageAnyActive(ControllerButtonUsage buttonUsage)
        {
            var currentData = GetData();
            return
                currentData.IsDataValid &&
                (buttonUsage & currentData.Input.ButtonUsageMask) != 0;
        }

        public virtual bool IsButtonUsageAllActive(ControllerButtonUsage buttonUsage)
        {
            var currentData = GetData();
            return currentData.IsDataValid &&
                   (buttonUsage & currentData.Input.ButtonUsageMask) == buttonUsage;
        }

        /// <summary>
        /// Retrieves the current controller pose, in world space.
        /// </summary>
        /// <param name="pose">Set to current pose if `IsPoseValid`; Pose.identity otherwise</param>
        /// <returns>Value of `IsPoseValid`</returns>
        public virtual bool TryGetPose(out Pose pose)
        {
            if (!IsPoseValid)
            {
                pose = Pose.identity;
                return false;
            }

            pose = GetData().Config.TrackingToWorldTransformer.ToWorldPose(GetData().RootPose);
            return true;
        }

        /// <summary>
        /// Retrieves the current controller pointer pose, in world space.
        /// </summary>
        /// <param name="pose">Set to current pose if `IsPoseValid`; Pose.identity otherwise</param>
        /// <returns>Value of `IsPoseValid`</returns>
        public virtual bool TryGetPointerPose(out Pose pose)
        {
            if (!IsPointerPoseValid)
            {
                pose = Pose.identity;
                return false;
            }

            pose = GetData().Config.TrackingToWorldTransformer.ToWorldPose(GetData().PointerPose);
            return true;
        }

        [SerializeField]
        private IDataSource<ControllerDataAsset> _modifyDataFromSource;

        private string logFilePath;

        private void Awake()
        {
            logFilePath = Path.Combine(Application.persistentDataPath, "ControllerTrackingLog.txt");

            // Create or clear the log file at the start
            File.WriteAllText(logFilePath, "Timestamp, Hand, X, Y, Z\n");
        }

        public override void MarkInputDataRequiresUpdate()
        {
            base.MarkInputDataRequiresUpdate();

            if (Started)
            {
                WhenUpdated();
                LogHandData();
            }
        }

        private void LogHandData()
        {
            if (TryGetPose(out Pose pose))
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string handName = Handedness.ToString();
                Vector3 position = pose.position;

                string logEntry = $"{timestamp}, {handName}, {position.x}, {position.y}, {position.z}\n";
                File.AppendAllText(logFilePath, logEntry);
            }
        }

        protected override void Apply(ControllerDataAsset data)
        {
            // Default implementation does nothing, to allow instantiation of this modifier directly
        }

        #region Inject

        public void InjectAllController(UpdateModeFlags updateMode, IDataSource updateAfter,
            IDataSource<ControllerDataAsset> modifyDataFromSource, bool applyModifier)
        {
            base.InjectAllDataModifier(updateMode, updateAfter, modifyDataFromSource, applyModifier);
            _modifyDataFromSource = modifyDataFromSource;
        }

        #endregion
    }
}
