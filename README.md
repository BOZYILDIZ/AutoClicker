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
- Actions : petit mouvement de souris, clic, Espace, avancer (Z/W), souris + Espace ou **Clavier**
- **Clavier** : un clavier complet s'affiche à côté, tu cliques sur la ou les touches à appuyer
  - une seule touche ou plusieurs à la suite (appuyées dans l'ordre des numéros)
  - claviers Windows et Mac, en AZERTY, QWERTY ou QWERTZ
  - durée d'appui de chaque touche et temps entre chaque touche réglables
- Arrêt automatique après X minutes / heures (ou jamais)
- Choix du clavier : AZERTY, QWERTY, QWERTZ (détecté automatiquement)
- Option « seulement si je suis inactif » : se met en pause tout seul quand tu joues

**Récolte (FiveM)**
- Appuie sur E (ou F, G, H, X, Espace) dès que le message de récolte apparaît au centre de l'écran
  (détection du cadre vert menthe du message, réglée pour le serveur testé)
- Pause réglable après chaque récolte, sensibilité basse / normale / haute
- Voyant en direct « détecté / pas visible » pour tester avant de lancer
- **Trajet enregistré** : **F9** pour enregistrer un tour du champ (touches + mouvements de souris),
  puis le perso le refait en boucle et s'arrête pour ramasser
- Le jeu doit être en « fenêtré sans bordure »

**Langues**
- Français (par défaut), anglais et turc : clique sur un drapeau en haut à droite
- Toute l'interface change de langue tout de suite, le choix est mémorisé

**Overlay en jeu**
- Notification à l'écran quand le programme s'active ou se désactive
- Indicateur permanent en haut à droite tant qu'il tourne

## Raccourcis

| Touche | Action |
|--------|--------|
| **F6** | Démarrer / arrêter |
| **F7** | Enregistrer le point fixe (position de la souris) |
| **F9** | Récolte : commencer / finir l'enregistrement du trajet |

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
