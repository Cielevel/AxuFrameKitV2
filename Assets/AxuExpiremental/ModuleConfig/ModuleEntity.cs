// ============================================
// 3. 主要的MonoBehaviour组件
// ============================================

using System.Collections.Generic;
using UnityEngine;

namespace AxuExpiremental.ModuleConfig
{
    public class ModularEntity : MonoBehaviour
    {
        [SerializeReference] // 重要：使用SerializeReference而不是SerializeField
        public List<ModuleBase> modules = new List<ModuleBase>();
    
        void Start()
        {
            foreach (var module in modules)
            {
                module.Initialize();
            }
        }
    
        void Update()
        {
            foreach (var module in modules)
            {
                module.Execute();
            }
        }
    }
}