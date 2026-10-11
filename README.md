# AutoClicker by BOZYILDIZ

Autoclicker et anti-AFK pour Windows, avec une interface sombre inspirée de macOS.

## Fonctionnalités

**AutoClicker**
- 1 clic toutes les X millisecondes / secondes / minutes / heures
- Mode **Hard Spam** (vitesse maximale, ~500 à 1000 clics par seconde)
- Bouton gauche, droit ou milieu — simple ou double clic
- Répétition infinie ou limitée à un nombre de clics
- Clic à la position de la souris ou sur un point fixe (enregistré avec **F7**)

**Anti-AFK**
- Agit toutes les X secondes / minutes pour éviter d'être kick
- Actions : petit mouvement de souris, clic, Espace, avancer (Z/W) ou souris + Espace
- Choix du clavier : AZERTY, QWERTY, QWERTZ (détecté automatiquement)
- Option « seulement si je suis inactif » : se met en pause tout seul quand tu joues

**Overlay en jeu**
- Notification à l'écran quand le programme s'active ou se désactive
- Indicateur permanent en haut à droite tant qu'il tourne

## Raccourcis

| Touche | Action |
|--------|--------|
| **F6** | Démarrer / arrêter |
| **F7** | Enregistrer le point fixe (position de la souris) |

## Utilisation

Télécharge `AutoClicker by BOZYILDIZ.exe` et lance-le : rien à installer (Windows 10 / 11).

Au premier lancement, Windows SmartScreen peut afficher un avertissement car le programme n'est pas signé :
clique sur **Informations complémentaires** puis **Exécuter quand même**.

La fenêtre du jeu doit être au premier plan pour que les clics et touches soient pris en compte.

## Compiler soi-même

Aucun SDK nécessaire, le compilateur C# est inclus dans Windows :

```bat
build.bat
```
