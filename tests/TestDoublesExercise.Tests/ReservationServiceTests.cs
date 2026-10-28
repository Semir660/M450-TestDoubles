using Moq;

namespace TestDoublesExercise.Tests;

public sealed class ReservationServiceTests
{
    // Entfernt Skip jeweils erst, wenn ihr den Test implementiert habt.
    // Die Beispiele für Stub, Spy und Moq stehen in TestDoubleExamples.cs.

    private sealed class StubAvailability(IReadOnlyDictionary<string, int> availabilityseats) : ISeatAvailability
    {
        public StubAvailability(string events, int availableSeats)
            : this( new Dictionary<string, int> { [events] = availableSeats } )
        {
        }
        
        public int GetAvailableSeats(string eventCode)
        {
            return availabilityseats.GetValueOrDefault(eventCode, 0);
        }
    }

    private sealed class Reposi(bool save = true) : IReservationRepository
    {
        public List<Reservation> SaveReservation { get; } = [];
        
        public bool TrySave(Reservation reservation)
        {
            SaveReservation.Add(reservation);
            return save;
        }
    }

    private sealed class Spys : IConfirmationSender
    {
        private readonly List<Reservation> _sentReser = [];
        
        public IReadOnlyList<Reservation> SentReser => _sentReser;
        
        public void Send(Reservation reservation)
        {
            _sentReser.Add(reservation);
        }
    }
    
    [Fact]
    public void Reserve_WithEnoughSeats_ReturnsTrue()
    {
        // Arrange: Stub mit 3 freien Plätzen, Repository mit erfolgreichem Speichern,
        //          eigener Sender-Spy und ReservationService.
        var ava = new StubAvailability("TEST 01", 3);
        var repo = new Reposi();
        var sendr = new Spys();
        var service = new ReservationService(ava, repo, sendr);
        
        // Act:     2 Plätze reservieren.
        var results = service.Reserve("lol.lol@lol.lol", "TEST 01", 2);
        
        // Assert:  Ergebnis ist true.
        Assert.True(results);
    }

    [Fact]
    public void Reserve_WithTooFewSeats_DoesNotSaveOrSend()
    {
        // Arrange: Stub mit 1 freiem Platz und Doubles für Repository und Sender.
        var ava = new StubAvailability("TEST 02", 1);
        var repo = new Reposi();
        var sendr = new Spys();
        var service = new ReservationService(ava, repo, sendr);
        
        // Act:     2 Plätze reservieren.
        var results = service.Reserve("lol.lol@lol.lol", "TEST 02", 2);
        
        // Assert:  Ergebnis ist false; weder Speichern noch Senden wurde aufgerufen.
        Assert.False(results);
        Assert.Empty(repo.SaveReservation);
        Assert.Empty(sendr.SentReser);
    }

    [Fact]
    public void Reserve_WithExactlyEnoughSeats_Succeeds()
    {
        // Arrange: Stub mit genau 2 freien Plätzen.
        var ava = new StubAvailability("TEST 03", 2);
        var repo = new Reposi();
        var sendr = new Spys();
        var service = new ReservationService(ava, repo, sendr);
        
        // Act:     2 Plätze reservieren.
        var results = service.Reserve("lol.lol@lol.lol", "TEST 03", 2);
        
        // Assert:  Ergebnis ist true.
        Assert.True(results);
    }

    [Fact]
    public void Reserve_WithZeroSeats_ThrowsBeforeCallingDependencies()
    {
        // Arrange: Doubles, deren Aufrufe sich prüfen lassen.
        var ava = new StubAvailability("TEST 04", 3);
        var repo = new Reposi();
        var sendr = new Spys();
        var service = new ReservationService(ava, repo, sendr);
        
        // Act:     0 Plätze reservieren.
        
        
        // Assert:  ArgumentOutOfRangeException; keine Abhängigkeit wurde aufgerufen.
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Reserve("lol.lol@lol.lol", "TEST 04", 0));
    }

    [Fact]
    public void Reserve_WithNegativeSeats_ThrowsBeforeCallingDependencies()
    {
        // Arrange: Doubles, deren Aufrufe sich prüfen lassen.
        var ava = new StubAvailability("TEST 05", 3);
        var repo = new Reposi();
        var sendr = new Spys();
        var service = new ReservationService(ava, repo, sendr);
        
        // Act:     -1 Plätze reservieren.
        
        
        // Assert:  ArgumentOutOfRangeException; keine Abhängigkeit wurde aufgerufen.
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Reserve("lol.lol@lol.lol", "TEST 05", -1));
    }

    [Fact]
    public void Reserve_WithEmptyEventCode_ThrowsBeforeCallingDependencies()
    {
        // Arrange: Doubles, deren Aufrufe sich prüfen lassen.
        var ava = new StubAvailability("TEST 06", 3);
        var repo = new Reposi();
        var sendr = new Spys();
        var service = new ReservationService(ava, repo, sendr);
        
        // Act:     Mit leerem Veranstaltungscode reservieren.
        
        
        // Assert:  ArgumentException; keine Abhängigkeit wurde aufgerufen.
        Assert.Throws<ArgumentException>(() => service.Reserve("lol.lol@lol.lol", "", 2));
    }

    [Fact]
    public void Reserve_WithWhitespaceEmail_ThrowsBeforeCallingDependencies()
    {
        // Arrange: Handgeschriebene Doubles oder Moq gezielt auswählen.
        var ava = new StubAvailability("TEST 07", 3);
        var repo = new Reposi();
        var sendr = new Spys();
        var service = new ReservationService(ava, repo, sendr);
        
        // Act:     Mit einer E-Mail-Adresse aus Leerzeichen reservieren.
        
        
        // Assert:  ArgumentException; keine Abhängigkeit wurde aufgerufen.
        Assert.Throws<ArgumentException>(() => service.Reserve("     ", "TEST 07", 2));
    }

    [Fact]
    public void Reserve_WhenSavingFails_DoesNotSendConfirmation()
    {
        // Arrange: Stub mit genügend Plätzen, Repository liefert false, eigener Spy.
        var ava = new StubAvailability("TEST 08", 3);
        var repo = new Reposi(false);
        var sendr = new Spys();
        var service = new ReservationService(ava, repo, sendr);
        
        // Act:     Reservierung versuchen.
        var results = service.Reserve("lol.lol@lol.lol", "TEST 08", 2);
        
        // Assert:  Ergebnis ist false und der Spy hat keinen Aufruf aufgezeichnet.
        Assert.False(results);
        Assert.Empty(sendr.SentReser);
    }

    [Fact]
    public void Reserve_WithTooFewSeats_DoesNotCallRepository()
    {
        // Arrange: Stub mit zu wenigen Plätzen und Mock<IReservationRepository>.
        var ava = new StubAvailability("TEST 09", 3);
        var repo = new Mock<IReservationRepository>();
        var sendr = new Spys();
        var service = new ReservationService(ava, repo.Object, sendr);
        
        // Act:     Reservierung versuchen.
        var result = service.Reserve("lol.lol@lol.lol", "TEST 09", 4);
        
        // Assert:  Verify(..., Times.Never()) für TrySave.
        Assert.False(result);
        repo.Verify(rep => rep.TrySave(It.IsAny<Reservation>()), Times.Never());
    }

    [Fact(Skip = "Fortgeschrittene: ReserveBatch zuerst implementieren")]
    public void ReserveBatch_WhenCombinedSeatsExceedCapacity_DoesNotSaveOrSend()
    {
        // Arrange: Zwei Reservierungen für dieselbe Veranstaltung, die einzeln passen.
        // Act:     Nach Implementierung des Batch-Verfahrens beide zusammen reservieren.
        // Assert:  Ergebnis ist false; kein Batch-Speichern und keine Bestätigung.
        throw new NotImplementedException();
    }
}
