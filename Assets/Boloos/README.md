# Boloos

Bolera generada por codigo para Unity. No necesita ningun modelo ni textura
importada: las mallas y las texturas se crean desde el editor con medidas
reglamentarias (USBC).

## Como se usa

1. Copia la carpeta `Assets/Boloos` dentro de tu proyecto.
2. Menu **Boloos > Construir bolera**, elige el numero de pistas y pulsa
   *Construir en la escena abierta*.
3. Las mallas, materiales y texturas generados se guardan en
   `Assets/Boloos/Generated`, asi que sobreviven al recargar la escena.
4. Dale a Play. Los controles de prueba salen arriba a la izquierda:
   *Lanzar*, *Replantar* y los deslizadores de velocidad, punteria y efecto.
   Con el Input Manager clasico tambien valen `Espacio`, `R` y `Tab`.

Menu **Boloos > Auditar escena**: recorre la escena abierta y lista los objetos
que siguen siendo placeholders (cubos y esferas por defecto de Unity, objetos
sin malla y objetos con el material por defecto), y los deja seleccionados en
la jerarquia. Es la herramienta para encontrar los restos del bloqueo previo y
borrarlos antes de construir la bolera de verdad.

## Que construye

| Pieza | Que es, y no un placeholder |
|---|---|
| Superficie | Losa de 1,054 x 19,156 m con textura de 39 tablas, juntas y veta |
| Canaletas | Canal circular extruido de 9,25" de ancho y 1 7/8" de hondo, con su pared |
| Flechas | Las 7 flechas de mira en las tablas 5, 10, 15, 20, 25, 30 y 35 |
| Aproximacion | 16 pies de tarima antes de la linea de falta |
| Bolos | Torneados a partir del perfil reglamentario, 1,53 kg, centro de masas bajo |
| Bola | Esfera de 8,5" con los tres agujeros perforados en la malla, 7,26 kg |
| Foso | Suelo hundido, colchon del fondo, kickbacks y capucha del pinsetter |
| Retorno | Foso con trigger, acelerador, elevador, carril en U y estante de 5 bolas |

## Los scripts

**Runtime**

- `AlleySpec` — todas las medidas reglamentarias en metros. Cambia una y el
  resto se reconstruye coherente.
- `MeshBuilder` — cajas, torneados, esferas y extrusiones. El winding sigue el
  convenio de Unity (`Cross(v1-v0, v2-v0)` da la normal frontal), por eso las
  caras salen hacia fuera y no invisibles.
- `ProceduralTextures` — madera de pista, bolo con sus franjas, bola y moqueta.
- `AlleyMaterials` — crea materiales detectando Built-in, URP o HDRP.
- `AlleyFactory` — monta la jerarquia completa.
- `BowlingBall`, `BowlingPin`, `PinSet` — fisica y estado del juego.
- `BallReturn`, `BallRack`, `SpinningPart` — la maquina de retorno.
- `BowlingLane` — une las piezas de una pista y lanza la bola.
- `BoloosDemo` — controles de prueba.

**Editor**

- `BoloosBuilderWindow` — la ventana de construccion.
- `BoloosSceneAudit` — el detector de placeholders.
- `BoloosMaterialRepair` — el reparador de materiales rosas.
- `BoloosAutoRepair` — lo lanza solo al abrir, al importar y al compilar.

## Si todo se ve rosa fucsia

El rosa de Unity no es una textura que falte (eso sale blanco): es un material
cuyo shader es nulo o no compilo, pintado con `Hidden/InternalErrorShader`.

**No hay que hacer nada**: con estos scripts en el proyecto, la reparacion se
lanza sola al abrir Unity, al recompilar, al importar materiales y al compilar
una build. Solo escribe en consola si ha cambiado algo. Se desactiva en
**Boloos > Reparar materiales automaticamente**.

Cubre las tres causas, y las distingue antes de tocar nada:

1. **El proyecto es URP o HDRP pero no tiene el asset del pipeline asignado.**
   Es la causa mas comun y la mas facil de empeorar: convertir los materiales a
   `Standard` quitaria el rosa cargandose el proyecto entero. Se detecta por
   evidencia (hay un `RenderPipelineAsset` en `Assets/` y los materiales usan
   sobre todo shaders de URP o HDRP) y se arregla asignando el pipeline en
   Graphics y Quality Settings, sin convertir ni un material.
2. **Materiales con un shader de otro pipeline**, por ejemplo `Standard` dentro
   de un proyecto URP. Se les cambia el shader al Lit del pipeline activo
   conservando color, textura, tiling y acabado. Los valores se leen de
   `m_SavedProperties`, no del material vivo: un material con el shader roto no
   responde a `HasProperty` y la reparacion dejaria todo blanco.
   Nunca se tocan los de interfaz, sprites, texto, cielo ni los de `Packages/`.
3. **Rosa solo en la build, bien en el editor.** El shader se queda fuera al
   compilar: `Shader.Find` solo encuentra shaders incluidos en la build, y un
   material creado en tiempo de ejecucion no lo referencia desde ningun asset.
   Se anade solo a *Always Included Shaders* antes de cada build.

Los tres tienen tambien su menu manual: **Boloos > Reparar materiales rosas** y
**Boloos > Incluir shaders en la build**.

La forma limpia de no pisar el tercer caso es construir la bolera desde el
editor (**Boloos > Construir bolera**) en vez de en tiempo de ejecucion: asi los
materiales quedan guardados como assets en `Assets/Boloos/Generated` y sus
shaders entran solos en la build.

## Los agujeros de la bola

No son un truco visual: `AlleyFactory.DrillHoles` quita los triangulos que caen
en la boca de cada agujero, cose el borde irregular que queda a un aro circular
limpio y baja las paredes del taladro hasta el fondo. Las paredes van a una
segunda submalla con material oscuro, porque un agujero del color de la bola no
parece un agujero.

La empunadura es la convencional: los dos dedos a 2,4" entre si y el pulgar a
4,25", medido sobre la superficie. Si los acercas mas, las bocas se solapan y el
corte se pisa a si mismo.

## Como funciona el retorno de bolas

El trigger del foso detecta la bola, la congela, arranca las ruedas del
acelerador y el elevador, y la lleva por los puntos del recorrido
(foso, pie del elevador, cabeza del elevador, carril, estante). Al llegar, el
estante la guarda en su hueco y las bolas van deslizando hacia delante segun se
cogen. Si el estante esta lleno, la bola se suelta al final del carril en vez de
desaparecer.

Los puntos del recorrido son objetos normales de la escena: muevelos en el
editor y la bola sigue el camino nuevo. Se ven dibujados al seleccionar el
componente `BallReturn`.
