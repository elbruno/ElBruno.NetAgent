# ElBruno.NetAgent – Promotional Materials

## 1. Positioning

ElBruno.NetAgent is a small Windows tray app for people who live dangerously close to hotel Wi-Fi.

It monitors network quality and helps your machine prefer the best available connection.

Built with:

- .NET 10
- WPF
- Windows system tray
- Local-first logic
- No cloud dependency

## 2. Taglines

Options:

1. Smart network switching from the Windows tray.
2. Hotel Wi-Fi, meet your backup plan.
3. Because demos deserve better than bad Wi-Fi.
4. A tiny network agent for unstable connections.
5. Your Windows tray just got connectivity superpowers.

Recommended:

> Hotel Wi-Fi, meet your backup plan.

## 3. Short description

ElBruno.NetAgent is a lightweight Windows tray app built with .NET 10 that monitors connection quality and helps your PC prefer the best available network, including Wi-Fi, Ethernet, and USB tethering.

## 4. Twitter/X post option 1

```text
Hotel Wi-Fi: "Trust me bro."

Me: launches ElBruno.NetAgent 😎

A tiny .NET 10 Windows tray app that monitors network quality and helps your machine prefer the best connection: Wi-Fi, Ethernet, or USB tethering.

Because demos deserve better than bad Wi-Fi.
```

## 5. Twitter/X post option 2

```text
New tiny tool idea: ElBruno.NetAgent 🚀

A Windows tray app built with .NET 10 that watches your network quality and helps switch between hotel Wi-Fi and USB tethering when things get ugly.

No cloud.
No magic.
Just fewer demo disasters.
```

## 6. LinkedIn post

```text
Bad Wi-Fi is not just annoying. It can kill a live demo, a customer call, or a coding session.

That was the idea behind ElBruno.NetAgent: a small Windows tray application built with .NET 10 that monitors network quality and helps your machine prefer the best available connection.

The first scenario is simple:
- Hotel Wi-Fi is connected
- Phone is connected through USB-C tethering
- The app watches latency and connection quality
- When things get unstable, it helps switch to the better option

The interesting part is not only the utility itself, but also the architecture:
- WPF tray app
- Generic Host + dependency injection
- Network monitoring services
- Decision engine
- Safe-first network switching
- Local-first, no cloud dependency

Tiny tools are great learning projects.
And sometimes, they also save your demo 😄
```

## 7. Blog post draft

# Building ElBruno.NetAgent: Smart Network Switching from the Windows Tray

Hi!

There are two types of Wi-Fi in the world:

1. Wi-Fi that works.
2. Hotel Wi-Fi.

And if you travel, present, record demos, or join calls from random places, you already know the pain.

You are connected. Everything looks fine. Then the call starts, the demo loads, and suddenly the network decides to become interpretive dance.

So I started building **ElBruno.NetAgent**, a small Windows tray app that monitors network quality and helps the machine prefer the best available connection.

## The idea

The first scenario is very real:

- My laptop is connected to hotel Wi-Fi.
- My phone is connected through USB-C.
- USB tethering is available as a backup connection.
- I want Windows to prefer the better connection when things get ugly.

Not a huge enterprise platform.
Not a cloud service.
Just a practical little agent sitting in the tray.

## The stack

The app is built with:

- .NET 10
- WPF
- Windows system tray support
- Microsoft.Extensions.Hosting
- Dependency injection
- A small decision engine
- Local configuration
- File-based logging

## The architecture

The app is split into a few simple pieces:

- Network inventory service
- Network quality monitor
- Decision engine
- Network controller
- Tray UI
- Configuration service

The most important design decision is this:

> The decision engine must be pure logic.

That makes it easy to test, easy to reason about, and easy for AI coding tools to implement safely.

## Safe first

Changing network settings can be risky, so the first implementation starts with safe defaults:

- Auto mode disabled
- Dry-run mode enabled
- No adapter disabling
- No DNS changes
- No VPN changes

The app should explain what it wants to do before it actually does it.

## Why build this?

Because tiny tools are great.

They solve a real problem.
They are fun to build.
They are perfect for learning.
And sometimes they save your live demo.

More soon!
```

## 8. First comment with links

```text
Repo: https://github.com/elbruno/ElBruno.NetAgent
NuGet: https://www.nuget.org/packages/ElBruno.NetAgent
Blog: https://elbruno.com/
```

## 9. Hashtags

```text
#dotnet
#csharp
#wpf
#windows
#devtools
#productivity
#ElBruno
```

## Phase 11 — Promotional materials

Short launch blurb

ElBruno.NetAgent is available now: a lightweight .NET agent that helps developers automate local workflows, manage network reliability, and integrate tools across CI/CD and chat. Fast setup, local-first, and open-source — try it today: <repo_url>

Twitter / X post draft

Launching ElBruno.NetAgent — a lightweight .NET agent to automate local developer workflows and improve reliability for demos and CI: <repo_url> #dotnet #DevTools #OSS

LinkedIn post draft

ElBruno.NetAgent is a lightweight, local-first .NET agent for automating developer workflows and improving network reliability during demos and CI runs. Fast to set up, secure by design, and open for contributions. See the repo and get started: <repo_url>

Blog post outline

- Title + TL;DR
- Problem: unreliable networks & manual workflows
- Solution: what ElBruno.NetAgent solves
- Quickstart: install, run, and an example
- Architecture & security notes
- Community & next steps

Suggested hashtags

#dotnet #DevTools #Automation #OSS #DeveloperExperience

Release checklist

- [ ] Finalize README and links
- [ ] Add CHANGELOG entry (Phase 11)
- [ ] Tag/release on GitHub
- [ ] Attach social images to release
- [ ] Publish announcements (Twitter/X, LinkedIn, blog)
- [ ] Confirm CI artifacts

