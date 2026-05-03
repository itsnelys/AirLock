# 📘 Space Engineers Airlock System — Script Documentation

Ce script pour le bloc programmable de *Space Engineers* implémente un **système de SAS intelligent**, capable de gérer le cyclage automatique de l'air, la détection de fuites et le confinement automatique des zones compromises.

---

## 📂 Table des Matières

* [Initialisation et Configuration](https://www.google.com/search?q=%23-initialisation-et-configuration)
* [Système de Cyclage](https://www.google.com/search?q=%23-syst%C3%A8me-de-cyclage)
* [Protocoles d'Urgence et Confinement](https://www.google.com/search?q=%23-protocoles-durgence-et-confinement)
* [Interface LCD et Signaux](https://www.google.com/search?q=%23-interface-lcd-et-signaux)
* [Commandes Terminal](https://www.google.com/search?q=%23-commandes-terminal)

---

## 🚀 Initialisation et Configuration

Le système s'initialise en scannant les groupes de blocs définis dans les constantes du script. Il utilise un système de tags pour différencier plusieurs SAS sur une même grille.

### **Groupes de blocs requis**
| Nom du Groupe | Description |
| --- | --- |
| `AIRLOCK_VENTS` | Évent de ventilation pour le cyclage. |
| `AIRLOCK_DOORS_IN` | Portes menant à l'intérieur de la base. |
| `AIRLOCK_DOORS_OUT` | Portes menant à l'extérieur ou au hangar. |
| `AIRLOCK_LCD` | Écrans d'affichage du statut local. |

* **Tagging :** Utilisez le format `[SAS:NomDuSas]` dans le nom des blocs pour les assigner à un SAS spécifique.
* **Sondes :** Le tag `[PROBE]` sur un évent permet une lecture précise de l'O2 sans affecter la pressurisation.

---

## 🔫 Système de Cyclage

Le script automatise la transition entre les environnements pressurisés et dépressurisés.

### **Logique de fonctionnement**
1.  **Vérification :** S'assure qu'aucune porte n'est ouverte avant de commencer.
2.  **Interlock :** Verrouille les portes opposées pendant le cycle pour éviter toute décompression accidentelle.
3.  **Égalisation :** Active les évents jusqu'à atteindre les seuils configurés (`0.95f` pour pressurisé, `0.05f` pour dépressurisé).

---

## 🚨 Protocoles d'Urgence et Confinement

Le cœur sécuritaire du système repose sur la surveillance constante des secteurs.

* **Détection de Fuite :** Si le niveau d'O2 chute anormalement (seuil par défaut `20%`), le secteur passe en mode confinement.
* **Confinement (Containment) :** Verrouille les portes du secteur, coupe la ventilation non essentielle et active les alarmes visuelles (LED rouges clignotantes).
* **Mode Secure :** Une commande globale qui verrouille tous les SAS de la grille, idéal pour les situations de combat ou d'alerte intrusion.

---

## ⚙️ Commandes Terminal

Le script accepte diverses commandes via l'argument du bloc programmable :

| Commande | Action |
| --- | --- |
| `pressu` | Lance le cycle de pressurisation du SAS par défaut. |
| `depressu` | Lance le cycle de dépressurisation. |
| `secure` | Bascule l'état de sécurité globale (ON/OFF). |
| `contain <nom>` | Active ou désactive manuellement le confinement d'une pièce. |
| `urgence` | Déclenche immédiatement une alerte manuelle sur le SAS. |

---

## 🚪 Logique d'Affichage (LCD)

L'interface LCD fournit un retour visuel complet :
* **Statut O2 :** Affichage en pourcentage de l'oxygène interne.
* **État des portes :** Indique si les portes INT ou EXT sont verrouillées ou ouvertes.
* **Alertes :** Affiche la cause exacte d'une urgence (Fuite O2, Porte endommagée, Manuel).

---

## 📄 License & Author

Provided by **soraanoir/sundae https://github.com/soraanoir**.  
Free to use and modify, provided that credit is given to the author.