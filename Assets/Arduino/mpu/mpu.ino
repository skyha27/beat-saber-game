#include <Adafruit_MPU6050.h>
#include <Adafruit_Sensor.h>
#include <Wire.h>
#include <BluetoothSerial.h>

Adafruit_MPU6050 mpu;
BluetoothSerial serialBT;
float alpha = 0.01;
uint32_t prevTime = 0;
float compX = 0;

void setup() {
  Serial.begin(115200);
  serialBT.begin("ESP-32 (left)");
  Wire.begin();

  if (!mpu.begin()) {
    Serial.println("Error finding mpu.");
    while(1);
  }

  mpu.setGyroRange(MPU6050_RANGE_500_DEG);
  mpu.setAccelerometerRange(MPU6050_RANGE_8_G);
  mpu.setFilterBandwidth(MPU6050_BAND_21_HZ);
}

void loop() {
  sensors_event_t accel, gyro, temp;
  mpu.getEvent(&accel, &gyro, &temp);

  uint32_t timestamp = millis();
  float dt = (timestamp - prevTime) / 1000.0; // convert from ms to s
  prevTime = timestamp;

  // Raw gyro data converted to degrees
  float rateRoll = (float)gyro.gyro.x * 180.0 / PI;

  // Raw accel data in m/s^2
  float accX = (float)accel.acceleration.x;
  float accY = (float)accel.acceleration.y;
  float accZ = (float)accel.acceleration.z;

  // Rotation angles from accelerometer
  float angleRoll = atan(accY/sqrt(accX*accX+accZ*accZ))*1/(3.142/180);

  // Complementery filter application
  compX = alpha * (compX + rateRoll * dt) + (1.0 - alpha) * angleRoll;

  // Send data to Unity
  String payload = String(compX) + ",";
  Serial.println(payload);

  if (!serialBT.available()) {
    serialBT.println(payload);
  }
  delay(300);
}
