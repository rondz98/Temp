   M S B A M L   `   `   `  ����?  ;Temp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullclr-namespace:TempTemp  5   	       Temp.CurveCustomization   5   	   W SPresentationCore, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   $localclr-namespace:Temp     5   	     � Title$  Personalizzazione curva��5   	   $	��480��6)   $	��500��66    � 
ResizeMode$ NoResize=�6B   .��5	      �  � ColumnDefinitions 5
   
   �� 5      �� 5      �� 5      �� 5       � RowDefinitions 5   
   �� 5      $��40 �6   �� 5      .+�5   
   �� -   5         ��Name$ 
Add_Button��5      $��Aggiungi riga��6%   $	��100��6=   $
��Left=�6I   $��5q�6d   $��0��6o   6   �� 5   
   -   5         ��Name$ Filename_Label��5      $��Nome curva: ��6(   $��0��6?   $��1��6L   $��Center=�6\   $��Center=�6~   �� 5   
   -   5         ��Name$ FileName_TextBox��5      $��0��6,   $��2��69   $��Center=�6I   $��
-25,5,30,5q�6k   6   �� 5   
   -   5        $ FilenameExt_Label��5      $
��.ccw��6+   $��0��6:   $��2��6G   $��Center=�6W   $��Right=�6y   �� 5   
   -   5        $ Save_Button��5      $��Salva��6&   $��50��66   $��Right=�6A   $��5q�6]   $��3��6h   6x   � 5   
   $��1��6   $��0��6   $��4��6,    5      �� 5      $��30 �6    �� 5       5      �� 5      $��* �6#   .+�5"      � -   5         �Name$ 
Title_grid��5"      $��0��6'   $��30��64   $	��Top=�6@    5#      �� 5$      $��50 �6'   �� 5%      $��85 �6'   �� 5&      $	��110 �6'   �� 5'      $	��100 �6'    5)      �� 5*      .+�5,      �� $��Id��6   $��Center=�6%   $��Center=�6I   $��0��6k   �� 5-      $��Temperatura��6   $��Center=�6.   $��Center=�6R   $��1��6t   �� 5.      $��Tempo��6   $��Center=�6(   $��Center=�6L   $��2��6n   50      �� 51      -   5         ��Name$ 
steps_grid��51      $��1��6*   $��0��67   $��Stretch=�6G   53      54      55      