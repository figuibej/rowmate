# Avisos de terceros

RowMate es MIT (ver `LICENSE`). Este archivo reúne los avisos de código de terceros incluido en la app; viaja con cada build
(06a-escena-lago/02-tech-design §2.10; QA-G6aT-19).

## Dart SDK — generador de números pseudoaleatorios

`Assets/RowMate/Core/Scene/DartRandom.cs` es un puerto a C# del `_Random` de la VM de Dart (`sdk/lib/_internal/vm/lib/math_patch.dart`):
la mezcla de la semilla de Thomas Wang, el generador *Multiply with Carry* de base 2³² y el algoritmo de `nextInt`, `nextDouble` y
`nextBool`. Se porta para que la orilla del lago sea, bit a bit, la de la app anterior (Q-61, ADR 012). Licencia BSD-3 del SDK de Dart:

```
Copyright 2012, the Dart project authors.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are
met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above
      copyright notice, this list of conditions and the following
      disclaimer in the documentation and/or other materials provided
      with the distribution.
    * Neither the name of Google LLC nor the names of its
      contributors may be used to endorse or promote products derived
      from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
```
