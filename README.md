# Digital Twin for FDM 3D Printing
---
#### Video Demo: https://drive.google.com/file/d/1ujDId2BaZMvme08-_M2RYOQVkTOmdAvm/view?usp=sharing

#### Parallel Codebase: https://github.com/timothytenzin20/FDM_WebSocket

---
### System Architecture

The system is composed of four primary layers that work together to mirror physical printer behavior in a virtual environment:

- **Unity Simulation**  
  Handles G-code parsing, printer logic, and real-time visualization of extrusion movements.

- **Raspberry Pi**  
  Hosts data transmission and forwards commands to the Arduino.

- **Arduino**  
  Acts as the interface between the Raspberry Pi and the local machine running the Unity simulation.

- **React Dashboard**  
  A web-based UI for monitoring printer metrics such as temperature and speed via WebSockets.

---

### Real-Time Monitoring

The dashboard provides visual alerts based on printer status:

- **Temperature**
- **Speed**
- **Quality**
