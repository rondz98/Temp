double temp_increment = 0.17;
double temp_decrement = 0.1;
double ambient_temp = 20; 

String incomingString;
byte buffer[20];
double temp = ambient_temp;
bool active = false;
bool previousActive = false;

unsigned long previousMillis = 0;
const long interval = 1000;

unsigned long previousTempChangeMillis = 0;
const long tempChangeDelay = 120000;

void setup() {
  Serial.begin(115200);
  pinMode(LED_BUILTIN, OUTPUT);
}  

void loop() {
  unsigned long currentMillis = millis();
  
  if (currentMillis - previousMillis >= interval) {
    previousMillis = currentMillis;

    if (currentMillis - previousTempChangeMillis >= tempChangeDelay) {
      if (active) {
        temp += temp_increment;
      } else {
        temp -= temp_decrement;
        if (temp < ambient_temp) {
          temp = ambient_temp;
        }
      }
      previousTempChangeMillis = currentMillis;
    }
  }
  
  // Legge la seriale
  if (Serial.available() > 0) {
    incomingString = Serial.readStringUntil('\n');
    incomingString.toCharArray(buffer, incomingString.length() + 1);
    
    if ((buffer[0] == 82)) { // R
      if (active) {
        Serial.print((int)temp);
        Serial.println(";1");
      } else {
        Serial.print((int)temp);
        Serial.println(";0");
      }
    } else {
      if ((buffer[0] == 87) && (buffer[1] == 59)) { // W;
        if (buffer[2] == 49) {
          Serial.println("S;1");
          digitalWrite(LED_BUILTIN, HIGH);
          active = true;
        } else {
          Serial.println("S;0");
          digitalWrite(LED_BUILTIN, LOW);
          active = false;
        }

        if (active != previousActive) {
          previousActive = active;
          previousTempChangeMillis = currentMillis;
        }
      }
    }
  }
}