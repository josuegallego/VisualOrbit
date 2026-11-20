# Inside Your Mind (Visual Orbit)

Experiencia educativa de realidad virtual para **Meta Quest 3**, desarrollada en Unity para la clase de **Diseño Multimedia 2** y dirigida a estudiantes de **Ingeniería Multimedia de séptimo y octavo semestre**. Proyecto desarrollado en el marco de Visual Orbit / Universidad Autónoma de Occidente (UAO).



## 1\. Descripción del sistema

**Inside Your Mind** es una experiencia inmersiva en realidad virtual que muestra a los estudiantes cómo sus **decisiones alimenticias durante un día** influyen en su **memoria a corto plazo**.

### Flujo de la experiencia

La experiencia recorre **tres escenarios que corresponden a las comidas de un mismo día**:

1. **Desayuno**
2. **Almuerzo**
3. **Cena**

En cada escenario el usuario toma decisiones alimenticias y juega **minijuegos**. La **dificultad de los minijuegos cambia según las decisiones alimenticias** que el usuario haya tomado: las elecciones que favorecen la memoria a corto plazo y las que la perjudican se reflejan directamente en el reto que enfrenta.

### Minijuegos

La experiencia cuenta con dos minijuegos:

* **Memorama**
* **Moléculas**

### Retroalimentación

Una **enfermera virtual** da una retroalimentación personalizada según las **elecciones del usuario** y el estado de las **barras** del sistema, de modo que el estudiante entiende el efecto de cada decisión.

### Características técnicas

* **Flujo basado en escenas:** cada comida es una escena independiente enlazada con las demás.
* **Arquitectura por capas:** gestores persistentes (*managers*) que se mantienen entre escenas y datos de configuración en `ScriptableObjects`.
* **Funcionamiento sin conexión:** no requiere internet.
* **Persistencia local:** los datos se guardan de forma local en el dispositivo.
* **Interacción con controles:** se maneja únicamente con los controles del visor (gatillo y botones); no usa seguimiento de manos.
* **Multiplataforma de uso:** está pensado para usarse como **APK en las gafas Meta Quest 3** y en **PC mediante el simulador XR de Unity**. También puede usarse **sin tener Unity instalado** (ver sección 4).



## 2\. Tecnologías utilizadas

|Tecnología|Uso en el proyecto|
|-|-|
|**Unity 6000.5.6f1**|Motor de desarrollo y ejecución de la experiencia|
|**C#**|Lenguaje de programación de la lógica del sistema|
|**Unity XR Interaction Toolkit**|Interacción en realidad virtual (controladores, selección) y simulador XR|
|**Meta XR SDK**|Integración con el visor Meta Quest 3|
|**ScriptableObjects**|Configuración y datos de la experiencia desacoplados de la lógica|
|**Almacenamiento local**|Guardado de datos en el dispositivo|
|**Visual Studio / VS Code**|Entorno de edición de código (`.vsconfig`, `.vscode`)|
|**Figma**|Prototipo de diseño que sirvió de base para la arquitectura de escenas|



## 3\. Requisitos mínimos del equipo

El demo está pensado para ejecutarse en computadores con:

* **Sistema operativo:** Windows 10 o superior.
* **Memoria RAM:** al menos 8 GB.
* **Tarjeta gráfica:** compatible con DirectX 11.
* **Conexión a internet:** no es necesaria; la experiencia guarda los datos de forma local.

Según la forma de ejecución se requiere además:

|Forma de ejecución|Requisitos adicionales|
|-|-|
|Visor (APK)|**Meta Quest 3** y un computador con Windows para instalar el APK|
|PC con simulador XR|**Unity Hub** y el editor **Unity 6000.5.6f1**, teclado y mouse|
|Sin Unity|Solo el APK (visor) o la compilación para Windows (PC), sin instalar Unity|
|Generar el APK desde Unity|Módulo **Android Build Support** (con OpenJDK y Android SDK \& NDK Tools)|



## 4\. Instrucciones para ejecutar el demo

Hay tres formas de ejecutar el demo.

|Opción|¿Requiere Unity?|¿Requiere visor?|
|-|-|-|
|[4.1 APK en el visor Meta Quest 3](#41-apk-en-el-visor-meta-quest-3)|No|Sí|
|[4.2 PC sin Unity](#42-pc-sin-unity)|No|No|
|[4.3 PC con el simulador XR de Unity](#43-pc-con-el-simulador-xr-de-unity-teclado-y-mouse)|Sí|No|

### 4.1 APK en el visor Meta Quest 3

No requiere tener Unity instalado.

1. **Descargue la carpeta completa del proyecto** desde el repositorio (**Code → Download ZIP** y descomprímala por completo, o `git clone https://github.com/josuegallego/VisualOrbit.git`).
2. Localice el archivo **`.apk`** de la experiencia. Si el repositorio no lo incluye, genérelo con la sección 4.4.
3. Active el **modo desarrollador** del visor desde la aplicación móvil de Meta Quest.
4. Conecte el visor al computador con un cable USB-C y acepte la depuración USB dentro del visor.
5. Instale el APK con una de estas opciones:

   * **Meta Quest Developer Hub:** arrastre el `.apk` a la ventana de la aplicación.
   * **SideQuest:** conecte el visor y use *Install APK file*.
   * **ADB:**

```bash
     adb install -r InsideYourMind.apk
     ```

6. En el visor, abra Biblioteca → Orígenes desconocidos y ejecute Inside Your Mind.

#### Controles en el visor (solo controles Meta Quest Touch)

La experiencia se maneja únicamente con los controles; no usa seguimiento de manos.

|Acción|Control|
|-|-|
|Apuntar a botones y objetos|Mover el control (rayo de apuntado)|
|Seleccionar / interactuar / confirmar|Gatillo|
|Abrir la configuración|Botón Y |

### 4.2 PC sin Unity

No requiere tener Unity instalado.

1. Descargue la compilación para Windows de la experiencia (carpeta `Build/` o el archivo `.zip` publicado en la sección Releases del repositorio).
2. Descomprima la carpeta completa. No ejecute el `.exe` desde dentro del ZIP ni lo mueva fuera de su carpeta: necesita los archivos `Player_Data` y `UnityPlayer.dll` que lo acompañan.
3. Ejecute InsideYourMind.exe.
4. Use los controles de la sección 4.3, que son los mismos del simulador.

### 4.3 PC con el simulador XR de Unity (teclado y mouse)

Esta opción no requiere visor. Utiliza el XR Device Simulator de Unity XR Interaction Toolkit para emular el visor y los controles con el teclado y el mouse.

1. Descargue la carpeta completa del proyecto (no solo `Assets`: también se necesitan `Packages` y `ProjectSettings`).
2. En Unity Hub, useAdd → Add project from disk y seleccione la carpeta descargada.
3. Ábralo con Unity 6000.5.6f1 (instálela desde Unity Hub si no la tiene) y espere a que importe los recursos. La primera vez puede tardar varios minutos.
4. Abra la escena inicial de la experiencia y verifique que contenga el prefab XR Device Simulator (se importa desde Package Manager → XR Interaction Toolkit → Samples).
5. Presione Play.

#### Controles del simulador XR (teclado y mouse)

Se usan las teclas por defecto del XR Device Simulator.

|Acción|Control|
|-|-|
|Moverse|`W` `A` `S` `D`|
|Subir / bajar|`E` / `Q`|
|Girar la cabeza (cámara)|Mover el mouse manteniendo presionado el botón derecho|
|Controlar el control izquierdo|Mantener `Shift izquierdo`|
|Controlar el control derecho|Mantener `Espacio`|
|Apuntar con el control|Mover el mouse|
|Gatillo (seleccionar / interactuar)|Clic izquierdo del mouse|
|Abrir la configuración|`F1`|

### 4.4 Generar el APK o la compilación de Windows desde Unity

1. Abra el proyecto como se indica en 4.3.
2. APK (Meta Quest 3):

   File → Build Profiles, seleccione Android y haga clic en Switch Platform.
   En Project Settings → XR Plug-in Management, habilite el proveedor Oculus para Android.
   Haga clic en Build (o *Build And Run con el visor conectado).
3. Windows (PC sin Unity):

   File → Build Profiles, seleccione Windows y haga clic en Build.
   Distribuya la carpeta completa generada.

## 5. Estructura del repositorio

```

VisualOrbit/
├── Assets/                  # Escenas, scripts, prefabs, ScriptableObjects y recursos
├── Packages/                # Dependencias del proyecto
├── ProjectSettings/         # Configuración del proyecto Unity
├── .vscode/                 # Configuración de VS Code
├── .vsconfig                # Componentes requeridos de Visual Studio
├── InsideYourMind2.0.slnx   # Solución de C#
├── .gitattributes
└── .gitignore

```

## 6. Créditos

Proyecto desarrollado para la clase de Diseño Multimedia 2, en el marco de Visual Orbit y la Universidad Autónoma de Occidente (UAO), Cali, Colombia.

Desarrolladores:

 Josué Santiago Gallego
 Estefanía del Amor Restrepo
 Daniel Muñoz
 Valentina Montero
 Mariana Parra



