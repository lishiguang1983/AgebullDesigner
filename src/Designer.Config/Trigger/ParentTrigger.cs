using Agebull.EntityModel.Config;

namespace Agebull.EntityModel.Designer
{

    /// <summary>
    /// ParentTrigger触发器
    /// </summary>
    public class ParentConfigTrigger : ConfigBaseTriggerEx<ParentConfigBase>
    {
        /// <summary>
        /// 载入事件处理
        /// </summary>
        protected override void OnLoad()
        {
            if (TargetConfig.MyChilds == null)
                return;
            foreach (var child in TargetConfig.MyChilds)
                GlobalTrigger.OnLoad(child);
        }
    }
}