okay ich habe nochmal etwas genauer nachgeforscht.
das hier ist der inventory slot bei einem verkauf:
count;instruction
53;7FF6E1A8EDAF - 8B 41 14  - mov eax,[rcx+14]
347;7FF6E1A8E6C2 - 39 41 14  - cmp [rcx+14],eax
2;7FF6E20CB57B - 8B 42 14  - mov eax,[rdx+14]
2;7FF6E1A8E77B - 48 89 51 10  - mov [rcx+10],rdx
1;7FF6E1A8EE10 - 89 51 14  - mov [rcx+14],edx
1;7FF6E1A8EE19 - 89 41 14  - mov [rcx+14],eax


und das mit dem itemstack(65):
count;instruction
4;7FF6E28245E6 - 39 86 A4020000  - cmp [rsi+000002A4],eax
1;7FF6E28245EE - 89 86 A4020000  - mov [rsi+000002A4],eax
1;7FF6E2824E46 - 41 39 86 A4020000  - cmp [r14+000002A4],eax


neu:
inventory:
7FF6E1A8EDAF - 8B 41 14  - mov eax,[rcx+14]
7FF6E1A8E6C2 - 39 41 14  - cmp [rcx+14],eax
7FF6E20CB57B - 8B 42 14  - mov eax,[rdx+14]
7FF6E1A8E77B - 48 89 51 10  - mov [rcx+10],rdx
7FF6E1A8EE10 - 89 51 14  - mov [rcx+14],edx
7FF6E1A8EE19 - 89 41 14  - mov [rcx+14],eax

slot:
7FF6E28245E6 - 39 86 A4020000  - cmp [rsi+000002A4],eax
7FF6E28245EE - 89 86 A4020000  - mov [rsi+000002A4],eax
7FF6E2824E46 - 41 39 86 A4020000  - cmp [r14+000002A4],eax


Headers	140000000	1400003ff	0x400	true	false	false	false	false		Default	true	ffxiv_dx11.exe[0x0, 0x400]		
.text	140001000	141efb9ff	0x1efaa00	true	false	true	false	false		Default	true	ffxiv_dx11.exe[0x400, 0x1efaa00]		
.rdata	141efc000	1425cf3ff	0x6d3400	true	false	false	false	false		Default	true	ffxiv_dx11.exe[0x1efae00, 0x6d3400]		
.data	1425d0000	142ca5fff	0x6d6000	true	true	false	false	false		Default	true	ffxiv_dx11.exe[0x25ce200, 0x4d000] | init[0x689000]		
.pdata	142ca6000	142e735ff	0x1cd600	true	false	false	false	false		Default	true	ffxiv_dx11.exe[0x261b200, 0x1cd600]		
_RDATA	142e74000	142e741ff	0x200	true	false	false	false	false		Default	true	ffxiv_dx11.exe[0x27e8800, 0x200]		
.rsrc	142e75000	14344bdff	0x5d6e00	true	false	false	false	false		Default	true	ffxiv_dx11.exe[0x27e8a00, 0x5d6e00]		
.reloc	14344c000	1434ed7ff	0xa1800	true	false	false	false	false		Default	true	ffxiv_dx11.exe[0x2dbf800, 0xa1800]		
tdb	ff00000000	ff0000184f	0x1850	true	true	false	false	true		Default	true	init[0x1850]

inv:
7FFB1EEB4E47 - 4E 8B 14 C1   - mov r10,[rcx+r8*8]

item:
7FFB1EEB4E4D - 4E 3B 14 C2   - cmp r10,[rdx+r8*8] ???????????????

7FF6DAE8F0DB - 48 89 51 10  - mov [rcx+10],rdx ????????????

7FF6DAE8F70F - 8B 41 14  - mov eax,[rcx+14]

