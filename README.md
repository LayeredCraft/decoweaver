# 🪓 Sculptor

**Shape your .NET dependency graph at compile time.**  
Sculptor uses .NET’s interceptor infrastructure to enable **compile-time decorator registration** — eliminating runtime assembly scanning, reflection, and manual factory wiring.

---

## ✨ Overview

Sculptor lets you define decorators declaratively on your service implementations.  
At build time, interceptors rewrite your DI registrations to apply decorators automatically, ensuring fast, reflection-free startup and clear service composition.
