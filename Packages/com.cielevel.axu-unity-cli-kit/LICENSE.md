MIT License

Copyright (c) 2026 Cielevel

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

---

## Third-party notices

This package **does not redistribute** any Unity-authored skill content.

- The `unity` CLI and the `com.unity.pipeline` package are Unity products, governed by
  their own licenses. This package only documents how to call them.
- The official skills at
  [Unity-Technologies/skills](https://github.com/Unity-Technologies/skills) are licensed
  under the Unity Companion License. They are **fetched on the consuming machine** — by
  `unity skill install <client>` or `npx skills add Unity-Technologies/skills` — and are
  never copied into this package. See `Documentation~/06-troubleshooting.md` if an
  official skill is missing.
- The AWS-style `skills.sh` folder convention (`SKILL.md` + `references/`, YAML
  frontmatter with `name` / `description` / `allowed-tools`) is a public convention shared
  by many vendors; the text in `Skills~/` here is written for this package.
