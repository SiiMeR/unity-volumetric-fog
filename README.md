# Volumetric Fog
Volumetric fog implementation in Unity. Made as a part of my Bachelors thesis.

# How to run:
1. Clone this repository
2. Download Unity(tested on versions 2018.1, 2017.4, 2017.3)
3. Open the project in Unity
4. Open the scene 'Forest' and press play
5. Fog settings can be changed by selecting the 'CameraAndFog' gameobject, which is a child of the 'Player' gameobject in the scene hierarchy

# Enabling benchmark mode
To enable benchmark mode, enable the Animator and Benchmark components under the 'Player' gameobject and disable the Player script. Additionally, enable the 'Text' gameobject under the 'Canvas' gameobject. Benchmark results are saved to AppData/LocalLow/DefaultCompany/unity-volumetric-fog on Windows.

# Default controls
W,S,A,D - Fly around
Mouse look - Look around
Shift - Sprint
