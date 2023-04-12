#include <LiquidCrystal_I2C.h>
#include <GyverMAX6675.h>


#define DATA_PIN  2
#define CS_PIN    3
#define CLK_PIN   4
#define RELAY     5
#define BUTTON    6
#define POT       A0

GyverMAX6675<CLK_PIN, DATA_PIN, CS_PIN> sens;
LiquidCrystal_I2C lcd(0x27, 16, 2);

String incomingString;
byte buffer[20];
int temp=0;
bool active=false;

int tempwanted=0;

bool isStandAlone = true;
bool buttonStatus = false;
bool oldbuttonStatus = false;

void setup() {
  pinMode(BUTTON, INPUT_PULLUP);
  pinMode(LED_BUILTIN, OUTPUT);

  lcd.begin();
  lcd.backlight();

  Serial.begin(115200);
  pinMode(5,OUTPUT);
}

void loop() {

  tempwanted = analogRead(POT);
  
  tempwanted = map(tempwanted, 0, 690, 0, 200);
  buttonStatus = digitalRead(BUTTON);
  
  if(buttonStatus != oldbuttonStatus){
    if(!buttonStatus){
      isStandAlone = !isStandAlone;
      lcd.begin();
    }
  }
  oldbuttonStatus = buttonStatus;

  if(isStandAlone){
    if (sens.readTemp()) {
      temp=sens.getTemp();
    }
    lcd.setCursor(0, 0);
    lcd.print("T. ATTUALE: ");
    lcd.print(temp);
    lcd.print("    ");
    lcd.setCursor(0, 1);
    lcd.print("T. VOLUTA:  ");
    lcd.print(tempwanted*10);
    lcd.print("    ");
    if(temp<tempwanted*10){
      digitalWrite(RELAY, HIGH);
      digitalWrite(LED_BUILTIN, HIGH);
      active=true;
    }else{
      digitalWrite(RELAY, LOW);
      digitalWrite(LED_BUILTIN, LOW);
      active=false;
    }
      
  }else{
    if (sens.readTemp()) {
      temp=sens.getTemp();
    }
    lcd.setCursor(0, 0);
    lcd.print("    MODALITA'");
    lcd.setCursor(0, 1);
    lcd.print("   AUTOMATICA");
    if (Serial.available() > 0) {
      incomingString = Serial.readStringUntil('\n');
      incomingString.toCharArray(buffer, incomingString.length() + 1);
      
      if ((buffer[0] == 82)){//R
          if(active)
            Serial.println(temp+";1");
          else
            Serial.println(temp+";0");
      }else{
        if ((buffer[0] == 87) && (buffer[1] == 59)){    //W;
          if(buffer[2] == 49){
            Serial.println("S;1");
            digitalWrite(5, HIGH);
            active=true;
          }
          else{
            Serial.println("S;0");
            digitalWrite(5, LOW);
            active=false;
          }
        }
      }
    }
  }
}