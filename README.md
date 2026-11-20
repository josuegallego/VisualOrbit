# Inside Your Mind — Visual Orbit

**Experiencia educativa de realidad virtual para Meta Quest 3**

Inside Your Mind es un proyecto desarrollado en **Unity** como parte de la asignatura **Diseño Multimedia 2** de la Universidad Autónoma de Occidente (UAO). Está dirigido a estudiantes de Ingeniería Multimedia de séptimo y octavo semestre y busca explorar la relación entre los hábitos alimenticios y la memoria a corto plazo mediante una experiencia inmersiva e interactiva.

---

## Tabla de contenidos

- [Descripción del proyecto](#descripción-del-proyecto)
- [Características principales](#características-principales)
- [Tecnologías utilizadas](#tecnologías-utilizadas)
- [Requisitos del sistema](#requisitos-del-sistema)
- [Ejecución del demo](#ejecución-del-demo)
  - [1. Meta Quest 3 (APK)](#1-meta-quest-3-apk)
  - [2. PC sin Unity](#2-pc-sin-unity)
  - [3. PC con simulador XR](#3-pc-con-simulador-xr)
  - [4. Generar compilaciones desde Unity](#4-generar-compilaciones-desde-unity)
- [Estructura del repositorio](#estructura-del-repositorio)
- [Créditos](#créditos)

---

## Descripción del proyecto

**Inside Your Mind** es una experiencia educativa en realidad virtual que permite explorar cómo las decisiones alimenticias tomadas durante un día pueden influir en el rendimiento de la memoria a corto plazo.

La experiencia se desarrolla a través de tres escenarios que representan las principales comidas del día:

1. **Desayuno:** primer escenario de la experiencia, donde el usuario comienza a tomar decisiones alimenticias y se enfrenta a los primeros retos de memoria.
2. **Almuerzo:** segundo escenario, en el que las decisiones acumuladas continúan influyendo en la dificultad de los desafíos.
3. **Cena:** último escenario del recorrido, donde se completa la experiencia y se refleja el impacto de las decisiones tomadas durante el día.

### Minijuegos

El proyecto incorpora dos actividades interactivas para evaluar y estimular la memoria:

| Minijuego | Descripción |
|---|---|
| **Memorama** | Actividad de asociación y reconocimiento de parejas de elementos. |
| **Moléculas** | Minijuego de reconocimiento y asociación de estructuras moleculares. |

### Retroalimentación personalizada

Una **enfermera virtual** acompaña al usuario durante la experiencia y proporciona retroalimentación de acuerdo con:

- Las decisiones alimenticias realizadas.
- El estado de las barras de seguimiento del sistema.
- Los resultados obtenidos durante los minijuegos.

La dificultad de los desafíos se adapta a las decisiones alimenticias, permitiendo que el usuario experimente cómo sus elecciones afectan el desarrollo de la actividad.

---

## Características principales

- **Realidad virtual inmersiva:** diseñada para Meta Quest 3.
- **Tres escenarios interactivos:** desayuno, almuerzo y cena.
- **Dificultad dinámica:** los minijuegos se adaptan según las decisiones alimenticias.
- **Retroalimentación personalizada:** acompañamiento mediante una enfermera virtual.
- **Arquitectura modular:** gestores persistentes (*Managers*) y configuración mediante ScriptableObjects.
- **Sistema basado en escenas:** cada comida corresponde a una escena independiente.
- **Funcionamiento offline:** no necesita conexión a internet.
- **Persistencia local:** los datos se almacenan directamente en el dispositivo.
- **Interacción mediante controladores:** utiliza los controles Meta Quest Touch, sin seguimiento de manos.
- **Dos modalidades de ejecución en PC:** compilación independiente o simulador XR de Unity.

---

## Tecnologías utilizadas

| Tecnología | Aplicación |
|---|---|
| **Unity 6000.5.6f1** | Motor de desarrollo y ejecución de la experiencia. |
| **C#** | Programación de la lógica del sistema. |
| **Unity XR Interaction Toolkit** | Interacciones en realidad virtual y simulación de dispositivos XR. |
| **Meta XR SDK** | Integración con Meta Quest 3. |
| **ScriptableObjects** | Gestión desacoplada de datos y configuraciones. |
| **Almacenamiento local** | Persistencia de información en el dispositivo. |
| **Visual Studio / VS Code** | Edición y desarrollo de scripts. |
| **Figma** | Prototipado de la experiencia y diseño de referencia para las escenas. |

---

## Requisitos del sistema

### Requisitos generales para PC

| Componente | Requisito |
|---|---|
| Sistema operativo | Windows 10 o superior. |
| Memoria RAM | 8 GB como mínimo. |
| Tarjeta gráfica | Compatible con DirectX 11. |
| Conexión a internet | No requerida para ejecutar la experiencia. |

### Requisitos según la modalidad de ejecución

| Modalidad | Requisitos adicionales |
|---|---|
| **Meta Quest 3 (APK)** | Visor Meta Quest 3 y computador Windows para instalar el APK. |
| **PC sin Unity** | Compilación para Windows. |
| **PC con simulador XR** | Unity Hub, Unity 6000.5.6f1, teclado y mouse. |
| **Generación de APK** | Android Build Support, OpenJDK y Android SDK & NDK Tools. |

---

## Ejecución del demo

Existen tres formas de ejecutar la experiencia, dependiendo del dispositivo disponible.

| Modalidad | ¿Requiere Unity? | ¿Requiere visor VR? |
|---|:---:|:---:|
| Meta Quest 3 (APK) | No | Sí |
| PC sin Unity | No | No |
| PC con simulador XR | Sí | No |

### 1. Meta Quest 3 (APK)

Esta modalidad permite ejecutar la experiencia directamente en el visor, sin necesidad de instalar Unity.

**Paso 1. Descargar el proyecto**

Descarga el repositorio mediante:

- `Code → Download ZIP`, y descomprime el archivo.
- O utiliza Git:

```bash
git clone https://github.com/josuegallego/VisualOrbit.git
```

**Paso 2. Localizar el APK**

Busca el archivo `.apk` correspondiente a Inside Your Mind.

Si el repositorio no incluye una compilación, puedes generarla siguiendo la sección [Generar compilaciones desde Unity](#4-generar-compilaciones-desde-unity).

**Paso 3. Activar el modo desarrollador**

Activa el modo desarrollador del visor desde la aplicación móvil de Meta Quest.

**Paso 4. Conectar el visor**

1. Conecta Meta Quest 3 al computador mediante un cable USB-C.
2. Colócate el visor y acepta la solicitud de depuración USB.

**Paso 5. Instalar el APK**

Puedes utilizar cualquiera de las siguientes herramientas:

- **Meta Quest Developer Hub:** arrastra el archivo `.apk` a la aplicación.
- **SideQuest:** conecta el visor y selecciona `Install APK file`.
- **ADB:** ejecuta el siguiente comando desde una terminal:

```bash
adb install -r InsideYourMind.apk
```

**Paso 6. Ejecutar la experiencia**

Desde el visor, ingresa a:

`Biblioteca → Orígenes desconocidos → Inside Your Mind`

#### Controles en Meta Quest 3

La experiencia utiliza exclusivamente los controles Meta Quest Touch.

| Acción | Control |
|---|---|
| Apuntar a botones y objetos | Movimiento del control mediante el rayo de apuntado. |
| Seleccionar e interactuar | Gatillo. |
| Confirmar acciones | Gatillo. |
| Abrir configuración | Botón Y. |

---

### 2. PC sin Unity

Esta modalidad permite ejecutar una compilación independiente para Windows sin instalar Unity ni disponer de un visor VR.

**Paso 1. Descargar la compilación**

Descarga la carpeta `Build/` o el archivo `.zip` publicado en la sección **Releases** del repositorio.

**Paso 2. Descomprimir los archivos**

Descomprime la carpeta completa de la compilación.

> Importante: no ejecutes el archivo `.exe` directamente desde el ZIP ni lo separes de los demás archivos. La aplicación necesita sus dependencias, como `Player_Data` y `UnityPlayer.dll`.

**Paso 3. Ejecutar el proyecto**

Abre:

```text
InsideYourMind.exe
```

**Paso 4. Interactuar con la experiencia**

Utiliza los controles del simulador XR descritos en la siguiente sección.

---

### 3. PC con simulador XR

Esta modalidad permite probar la experiencia desde un computador sin necesidad de un visor de realidad virtual.

Se utiliza el **XR Device Simulator**, incluido en Unity XR Interaction Toolkit, para emular el visor y sus controladores mediante teclado y mouse.

**Paso 1. Descargar el repositorio**

Descarga el proyecto completo. Es importante conservar las siguientes carpetas:

```text
Assets/
Packages/
ProjectSettings/
```

**Paso 2. Abrir Unity Hub**

1. Abre Unity Hub.
2. Selecciona `Add → Add project from disk`.
3. Busca y selecciona la carpeta del proyecto.

**Paso 3. Abrir el proyecto**

Abre el proyecto utilizando la versión:

```text
Unity 6000.5.6f1
```

Si no está instalada, puedes agregarla desde Unity Hub.

Espera a que Unity importe los recursos y dependencias. Este proceso puede tardar varios minutos durante la primera ejecución.

**Paso 4. Verificar el simulador XR**

Abre la escena inicial y verifica que incluya el prefab `XR Device Simulator`.

Si no está disponible:

1. Abre `Window → Package Management → Package Manager`.
2. Busca `XR Interaction Toolkit`.
3. Ingresa a la sección `Samples`.
4. Importa el ejemplo `XR Device Simulator`.

**Paso 5. Ejecutar**

Presiona el botón **Play** en el editor de Unity.

#### Controles del simulador XR

| Acción | Control |
|---|---|
| Moverse | `W` `A` `S` `D` |
| Subir / bajar | `E` / `Q` |
| Girar la cámara | Mantener clic derecho y mover el mouse. |
| Controlar el mando izquierdo | Mantener `Shift izquierdo`. |
| Controlar el mando derecho | Mantener `Espacio`. |
| Apuntar con el control | Mover el mouse. |
| Seleccionar / interactuar | Clic izquierdo. |
| Abrir configuración | `F1` |

---

### 4. Generar compilaciones desde Unity

Si necesitas generar tu propio APK para Meta Quest 3 o una compilación independiente para Windows, sigue estos pasos.

#### Generar APK para Meta Quest 3

1. Abre el proyecto en Unity.
2. Ingresa a:

```text
File → Build Profiles
```

3. Selecciona **Android**.
4. Presiona `Switch Platform`.
5. Ingresa a `Project Settings → XR Plug-in Management`.
6. Habilita el proveedor Oculus para Android.
7. Verifica que esté configurado el soporte de Android.
8. Presiona `Build` para generar el APK.

También puedes utilizar `Build And Run` si el visor está conectado al computador.

#### Generar compilación para Windows

1. Abre el proyecto en Unity.
2. Ingresa a:

```text
File → Build Profiles
```

3. Selecciona **Windows**.
4. Presiona `Build`.
5. Selecciona la carpeta de destino.
6. Distribuye la carpeta completa generada.

---

## Estructura del repositorio

La organización principal del proyecto es la siguiente:

```text
VisualOrbit/
│
├── Assets/
│   ├── Scenes/              # Escenarios de la experiencia
│   ├── Scripts/             # Lógica y funcionalidades en C#
│   ├── Prefabs/             # Objetos y componentes reutilizables
│   └── ...                  # Recursos y configuraciones
│
├── Packages/                # Dependencias del proyecto
├── ProjectSettings/         # Configuración de Unity
│
├── .vscode/                 # Configuración de Visual Studio Code
├── .vsconfig                # Componentes requeridos de Visual Studio
├── InsideYourMind2.0.slnx   # Solución de C#
├── .gitattributes
└── .gitignore
```

---

## Créditos

**Inside Your Mind** fue desarrollado en el marco de la asignatura **Diseño Multimedia 2**, dentro del proyecto Visual Orbit de la Universidad Autónoma de Occidente (UAO), Cali, Colombia.

### Equipo de desarrollo

- **Josué Santiago Gallego**
- **Estefanía del Amor Restrepo**
- **Daniel Muñoz**
- **Valentina Montero**
- **Mariana Parra**

---

<p align="center">
  <strong>Inside Your Mind · Visual Orbit</strong><br>
  Universidad Autónoma de Occidente<br>
  Cali, Colombia
</p>
