const int potPin = 5;  // Analog input pin for potentiometer

void setup() {
  Serial.begin(115200);  // Initialize serial communication
  delay(1000);           // Wait for serial to be ready
  Serial.println("ESP32-S3 Potentiometer Reader Started");
}

void loop() {
  // Read raw analog value (0-4095 for 12-bit ADC on ESP32)
  int rawValue = analogRead(potPin);
  
  // Convert to percentage (0-100)
  int percentage = map(rawValue, 0, 4095, 0, 100);
  
  // Convert to 8-bit value (0-255) if needed
  int byteValue = map(rawValue, 0, 4095, 0, 255);
  
  // Print values
  Serial.print("Raw: ");
  Serial.print(rawValue);
  Serial.print(" | Percentage: ");
  Serial.print(percentage);
  Serial.print("% | Byte: ");
  Serial.println(byteValue);
  
  delay(200);  // Update every 200ms
}
