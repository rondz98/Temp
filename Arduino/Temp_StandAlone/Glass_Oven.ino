#include <LiquidCrystal_I2C.h>
#include <GyverMAX6675.h>


#define DATA_PIN  D5
#define CS_PIN    D7
#define CLK_PIN   D6
#define RELAY     D3
#define BUTTON    D4
#define POT       A0

GyverMAX6675<CLK_PIN, DATA_PIN, CS_PIN> sens;
LiquidCrystal_I2C lcd(0x27, 16, 2);

String incomingString;
char buffer[20];
int temp=0;
bool active=false;

int tempwanted=0;
unsigned long previousMillisReadTemp = 0;
unsigned long previousMillisBlinkText = 0;
unsigned long previousMilliscommunication = 0;
const long intervalReadTemp = 500;
const long intervalBlinkText = 200;
const long timeoutAutoMode = 60000;

bool isStandAlone = true;
bool oldStandAloneStatus = true;
bool buttonStatus = false;
bool oldbuttonStatus = false;

bool isArmed = false;
bool textIsVisible = true;

void setup() {
  pinMode(BUTTON, INPUT_PULLUP);

  lcd.init();
  lcd.backlight();

  Serial.begin(115200);
  pinMode(RELAY,OUTPUT);
  digitalWrite(RELAY, LOW);
}

void loop() {
  unsigned long currentMillis = millis();

  if (Serial.available() > 0) {
    isStandAlone = false;
  }

  if(oldStandAloneStatus != isStandAlone){
    lcd.init();
    oldStandAloneStatus = isStandAlone;
  }

  if(currentMillis - previousMillisReadTemp >= intervalReadTemp){
    temp=sens.getTemp();
    previousMillisReadTemp = currentMillis;
  }
  
  if(isStandAlone){
    buttonStatus = digitalRead(BUTTON);
    if(buttonStatus != oldbuttonStatus){
      if(!buttonStatus){
        isArmed = !isArmed;
      }
    }
    oldbuttonStatus = buttonStatus;

    lcd.setCursor(0, 0);
    lcd.print("T. ATTUALE: ");
    lcd.print(temp);
    lcd.print("    ");

    if(isArmed){
      lcd.setCursor(0, 1);
      lcd.print("T. VOLUTA:  ");
      lcd.print(tempwanted*10);
      lcd.print("    ");
      if(temp<tempwanted*10){
        digitalWrite(RELAY, HIGH);
        active=true;
      }else{
        digitalWrite(RELAY, LOW);
        active=false;
      }
    }else{
      
      tempwanted = analogRead(POT);
    
      tempwanted = map(tempwanted, 1024, 0, 0, 200);
      digitalWrite(RELAY, LOW);
      active=false;
      if(currentMillis - previousMillisBlinkText >= intervalBlinkText){
        textIsVisible = !textIsVisible;
        previousMillisBlinkText = currentMillis;
      }
      if(textIsVisible){
        lcd.setCursor(0, 1);
        lcd.print("T. VOLUTA:  ");
        lcd.print(tempwanted*10);
        lcd.print("    ");

      }else{
        lcd.setCursor(0, 1);
        lcd.print("                ");
      }
    }
	previousMilliscommunication = currentMillis;
      
  }else{
    
    if (Serial.available() > 0) {
      lcd.setCursor(0, 0);
      lcd.print("    MODALITA'");
      lcd.setCursor(0, 1);
      lcd.print("   AUTOMATICA");

      incomingString = Serial.readStringUntil('\n');
      incomingString.toCharArray(buffer, incomingString.length() + 1);
      
      if ((buffer[0] == 82)){//R

          if(active){
            Serial.print(temp);
            Serial.println(";1");
          }
          else{
            Serial.print(temp);
            Serial.println(";0");
          }
      }else{
        if ((buffer[0] == 87) && (buffer[1] == 59)){    //W;
          if(buffer[2] == 49){ //1
            Serial.println("S;1");
            digitalWrite(RELAY, HIGH);
            active=true;
          }
          else{
            Serial.println("S;0");
            digitalWrite(RELAY, LOW);
            active=false;
          }
        }
      }
      previousMilliscommunication = currentMillis;
    }else{
      if(currentMillis - previousMilliscommunication >= timeoutAutoMode){
        digitalWrite(RELAY, LOW);
        active=false;
        isStandAlone = true;
      }
    }
  }
}