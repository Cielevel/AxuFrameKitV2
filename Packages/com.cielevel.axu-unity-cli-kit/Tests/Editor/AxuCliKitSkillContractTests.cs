using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AxuCliKit.Editor;
using NUnit.Framework;

namespace AxuCliKit.Tests.Editor
{
    /// <summary>
    /// Guards the package's contract with AI clients: the skill folder must satisfy the
    /// `SKILL.md` conventions, and the install targets must stay well-formed. Read-only — these
    /// tests never write into the project.
    /// </summary>
    [Category("Axu/Module/CliKit")]
    public class AxuCliKitSkillContractTests
    {
        private const string ExpectedSkill = "unity-cli-workflows";

        private static string SkillRoot
        {
            get { return Path.Combine(AxuCliKitPaths.SkillsRoot, ExpectedSkill); }
        }

        [Test]
        public void DiscoverSkills_包内技能目录_至少包含工作流技能()
        {
            var skills = AxuCliKitPaths.DiscoverSkills();

            Assert.Contains(ExpectedSkill, skills, "Skills~/ should contain the " + ExpectedSkill + " skill.");
            Assert.IsTrue(File.Exists(Path.Combine(AxuCliKitPaths.SkillsRoot, ExpectedSkill, "SKILL.md")),
                "A skill folder must have SKILL.md at its top level.");
        }

        [Test]
        public void SkillFrontmatter_技能元数据_名称与目录一致且描述非空()
        {
            var meta = AxuCliKitSkillMeta.Parse(File.ReadAllText(Path.Combine(SkillRoot, "SKILL.md")));

            Assert.AreEqual(ExpectedSkill, meta.Name, "frontmatter `name` must match the folder name.");
            Assert.IsTrue(Regex.IsMatch(meta.Name, "^[a-z0-9]+(-[a-z0-9]+)*$"),
                "frontmatter `name` must be kebab-case.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(meta.Description),
                "frontmatter `description` is the trigger — it cannot be empty.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(meta.Title), "SKILL.md must start with an `# H1` title.");
        }

        [Test]
        public void SkillReferences_每个参考文件_都被SKILL点md链接()
        {
            var body = File.ReadAllText(Path.Combine(SkillRoot, "SKILL.md"));
            var referenceDir = Path.Combine(SkillRoot, "references");

            if (!Directory.Exists(referenceDir))
                Assert.Ignore("This skill has no references/ folder.");

            var files = Directory.GetFiles(referenceDir, "*.md");
            Assert.Greater(files.Length, 0, "references/ exists but is empty.");

            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                Assert.IsTrue(body.Contains(name),
                    "SKILL.md never links references/" + name + " — a reference nobody reads is dead weight.");
            }
        }

        [Test]
        public void Targets_目标表_标识唯一且路径含技能占位符()
        {
            var seen = new HashSet<string>();

            foreach (var target in AxuCliKitPaths.Targets)
            {
                Assert.IsTrue(seen.Add(target.Id), "Duplicate target id: " + target.Id);

                // A managed block lives inside a shared file (AGENTS.md) and owns only a delimited
                // region of it, so it is the one kind that must NOT be skill-specific.
                if (target.Kind != AxuCliKitTargetKind.ManagedBlock)
                {
                    Assert.IsTrue(target.PathTemplate.Contains("{skill}"),
                        "Target '" + target.Id + "' has no {skill} placeholder — it would overwrite a shared path.");
                }

                var resolved = target.ResolvePath(ExpectedSkill);
                Assert.IsFalse(resolved.Contains("{skill}"), "Unresolved placeholder in " + resolved);
                Assert.IsFalse(Path.IsPathRooted(resolved), "Install targets must stay project-relative: " + resolved);
            }
        }

        [Test]
        public void Targets_目录式技能目标_覆盖已支持客户端的原生目录与跨工具通用目录()
        {
            var skillDirectories = new List<string>();
            foreach (var target in AxuCliKitPaths.Targets)
            {
                if (target.Kind == AxuCliKitTargetKind.SkillDirectory)
                    skillDirectories.Add(target.Id);
            }

            // These carry the full skill copy; the rest are rule/instruction pointers.
            Assert.Contains("claude-code", skillDirectories);
            Assert.Contains("zcode", skillDirectories);
            Assert.Contains("opencode", skillDirectories);
            Assert.Contains("agents-dir", skillDirectories);
            Assert.AreEqual("zcode", AxuCliKitPaths.FindTarget("zcode").Id);
            Assert.AreEqual("opencode", AxuCliKitPaths.FindTarget("opencode").Id);
            Assert.AreEqual(".opencode/skills/{skill}", AxuCliKitPaths.FindTarget("opencode").PathTemplate);
            Assert.AreEqual(".agents/skills/{skill}", AxuCliKitPaths.FindTarget("agents-dir").PathTemplate);
        }

        [Test]
        public void DetectTargets_探测结果_为空时回退到默认目标()
        {
            var detected = AxuCliKitSettings.DetectTargetsCsv();

            Assert.IsFalse(string.IsNullOrWhiteSpace(detected),
                "Detection must always return at least the fallback target.");
        }

        [Test]
        public void HardRules_技能守卫条款_可提取且非空()
        {
            var rules = AxuCliKitSkillMeta.HardRules(ExpectedSkill);

            Assert.IsFalse(string.IsNullOrWhiteSpace(rules));
            StringAssert.Contains("## Guardrails", rules,
                "The rules embedded in generated rule files come from SKILL.md's Guardrails section.");
        }

        [Test]
        public void PackageJson_包清单_声明名称与版本()
        {
            var manifest = Path.Combine(AxuCliKitPaths.PackageRoot, "package.json");
            Assert.IsTrue(File.Exists(manifest), "package.json is missing at " + manifest);

            var json = File.ReadAllText(manifest);
            StringAssert.Contains("\"name\": \"com.cielevel.axu-unity-cli-kit\"", json);
            StringAssert.Contains("\"version\"", json);
        }
    }
}
