// ============================================
// 1. 定义模块基类
// ============================================

using System;
using UnityEngine;

namespace AxuExperimental.ModuleConfig
{
    [Serializable]
    public abstract class ModuleBase
    {
        public abstract string GetModuleName();

        // 模块的业务逻辑
        public virtual void Initialize()
        {
        }

        public virtual void Execute()
        {
        }
    }

    // ============================================
    // 2. 具体的模块实现示例
    // ============================================
    [Serializable]
    public class HealthModule : ModuleBase
    {
        public float maxHealth = 100f;
        public float currentHealth = 100f;
        public bool autoRegenerate = false;

        public override string GetModuleName() => "Health Module";

        public override void Execute()
        {
            if (autoRegenerate && currentHealth < maxHealth)
            {
                currentHealth += Time.deltaTime * 5f;
            }
        }
    }

    [Serializable]
    public class MovementModule : ModuleBase
    {
        public float moveSpeed = 5f;
        public float jumpForce = 10f;
        public bool canDoubleJump = false;

        public override string GetModuleName() => "Movement Module";
    }

    [Serializable]
    public class AttackModule : ModuleBase
    {
        public float attackDamage = 10f;
        public float attackRange = 2f;
        public float attackCooldown = 1f;

        public override string GetModuleName() => "Attack Module";
    }

    [Serializable]
    public class ShieldModule : ModuleBase
    {
        public float shieldCapacity = 50f;
        public float rechargeRate = 5f;
        public float rechargeDelay = 3f;

        public override string GetModuleName() => "Shield Module";
    }
}