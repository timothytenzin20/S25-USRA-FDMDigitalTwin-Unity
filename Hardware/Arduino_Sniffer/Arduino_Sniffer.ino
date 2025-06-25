#include <SoftwareSerial.h>
#define rxPin 10
#define txPin 11

/* CAUTION: 3.3V RASPBERRY PI CAN ONLY BE MASTER TO SLAVE 5V ARDUINO */
SoftwareSerial PI_Serial(rxPin, txPin); // rx from pi, tx unused

void setup() {
  Serial.begin(115200);       // USB serial to unity
  PI_Serial.begin(115200);    // receiving from Pi
}

void loop() {
  while (PI_Serial.available()) {
    Serial.println(PI_Serial.read()); // forward command to Unity
  }

  // static unsigned long lastSent = 0;
  // if (millis() - lastSent > 1000) {
  //   Serial.println("hello world");
  //   lastSent = millis();
  // }
}
