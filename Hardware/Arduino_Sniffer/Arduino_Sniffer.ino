#include <SoftwareSerial.h>

SoftwareSerial softSerial(10, 11); // RX = Pin 10 (from Pi TX), TX = PIN 11 (unused)

void setup() {
  Serial.begin(115200);       // USB serial to Unity
  softSerial.begin(115200);   // Receiving from Pi
}

void loop() {
  while (softSerial.available()) {
    char c = softSerial.read();
    Serial.write(c);          // Forward to Unity over USB
  }
}
