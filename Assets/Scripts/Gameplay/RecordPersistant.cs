using System;
using SnakeSnack.Rules;
using UnityEngine;

namespace SnakeSnack.Gameplay
{
    /// <summary>
    /// Le record survit à la fermeture du jeu (GDD §4.5). Adaptateur de stockage, rien d'autre.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Meilleur effort, jamais bloquant.</b> En WebGL le stockage est lié à l'origine du site
    /// et peut disparaître (navigation privée, purge du navigateur) ; il peut aussi revenir abîmé,
    /// ou porter une valeur écrite par autre chose sous la même clé. Dans tous ces cas le record
    /// repart de zéro et le jeu démarre : il ne doit <b>jamais</b> refuser de se lancer pour un
    /// compteur.
    ///
    /// <para>⚠ <b><see cref="PlayerPrefs.Save"/> est appelé explicitement</b> à chaque écriture.
    /// Sans lui, la valeur ne vit qu'en mémoire jusqu'à une sortie propre du jeu — c'est-à-dire que
    /// l'onglet fermé en cours de partie, exactement le cas que le §4.5 veut couvrir, perdrait le
    /// record. Le coût est acceptable parce qu'on n'écrit qu'aux ticks où le record change vraiment
    /// (<see cref="Score.CompterUnePomme"/> le signale), pas à chaque pomme.</para>
    ///
    /// <para>Cette classe vit dans <c>Gameplay/</c> et non dans <c>Rules/</c> : elle touche au
    /// moteur, donc elle n'est pas testable hors Unity. Tout ce qui se décide — normalisation d'un
    /// record abîmé, comparaison, prédicat « record battu » — appartient à <see cref="Score"/>.</para>
    /// </remarks>
    public static class RecordPersistant
    {
        /// <summary>
        /// Clé de stockage. ⚠ <b>Nommée et stable</b> : la changer ferait repartir de zéro tous les
        /// joueurs, sans qu'aucun test ne tombe — leur record existerait encore, sous l'ancien nom.
        /// </summary>
        public const string Cle = "snakesnack.record";

        /// <summary>Le record connu, ou zéro s'il est absent, illisible ou abîmé.</summary>
        public static int Lire()
        {
            try
            {
                return Score.NormaliserRecord(PlayerPrefs.GetInt(Cle, 0));
            }
            catch (Exception erreur)
            {
                // La clé existe mais porte autre chose qu'un entier : PlayerPrefs lève. On repart de
                // zéro, on le journalise, et la partie commence quand même.
                Debug.LogWarning("[record] lecture impossible, repart de zero : " + erreur.Message);
                return 0;
            }
        }

        /// <summary>Écrit le record et le pousse sur le disque tout de suite.</summary>
        public static void Ecrire(int record)
        {
            try
            {
                PlayerPrefs.SetInt(Cle, Score.NormaliserRecord(record));
                PlayerPrefs.Save();
            }
            catch (Exception erreur)
            {
                // Un stockage indisponible (navigation privée, quota) ne doit pas interrompre la
                // partie en cours : le record vaudra ce qu'il vaut à la prochaine session.
                Debug.LogWarning("[record] ecriture impossible : " + erreur.Message);
            }
        }
    }
}
