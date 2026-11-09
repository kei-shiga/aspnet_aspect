---
name: extreme-token-reduction
description: '極めて高いトークン効率スキル。厳格なトークン予算内で動作し、内部思考、ツール使用、出力生成を最小限に抑える。'
user-invocable: true
---

# Extreme Token Efficiency
極めて高いトークン効率スキル

## Goal
エージェントに対し、厳格なトークン予算内で動作するように指示し、精度を損なうことなく、内部思考、ツール使用、および出力生成を最小限に抑えるようにする。

## Instructions
1. **Micro-Thoughts Only:** Keep internal reasoning and planning to an absolute minimum (1-2 sentence fragments maximum). Execute immediately once the solution path is identified.
2. **Targeted Retrieval:** Use targeted search or grep commands to extract specific lines rather than reading entire files or dumping full databases.
3. **Batch Execution:** Execute multiple independent tool calls concurrently in a single batch to avoid sequential overhead.
4. **Minimal Formatting:** Deliver raw answers, exact code blocks, or concise bullet points. Avoid paragraphs when a list will suffice.
5. **Partial Updates:** When modifying code or text, output only the specific lines changed (as a diff, patch, or search/replace block) rather than rewriting the unaltered file.

## Examples

**Input:**
"「`src/components/Auth.js` 内のログインボタンのテキストにあるタイプミスを修正します。」"

**Efficient Execution:**
```bash
sed -i 's/Loginn/Login/g' src/components/Auth.js
```

```diff
- <button>Loginn</button>
+ <button>Login</button>
```

**Inefficient Execution (To Avoid):**
*Thinking: The user wants to fix a typo. First, I will read the entire file. Then I will write a script to change it. Finally, I will output the whole file.*
"Hello! I can help with that. Here is the fully updated file with the typo corrected..." 
*(Outputs 500 lines of unchanged code)*

## Constraints
- **Do not** use conversational filler, greetings, apologies, transitions, or closing remarks.
- **Do not** echo or summarize the user's prompt before answering.
- **Do not** brainstorm, deliberate, or list multiple approaches unless explicitly required to solve a complex logic trap.
- **Do not** search, read, or query files, web pages, or databases outside the absolute minimum scope required for the task at hand.
- **Do not** make exploratory or "just in case" tool calls. Simplify your work by all means necessary.
