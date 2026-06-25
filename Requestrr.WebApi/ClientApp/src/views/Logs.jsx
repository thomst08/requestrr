/*!

=========================================================
* Argon Dashboard React - v1.0.0
=========================================================

* Product Page: https://www.creative-tim.com/product/argon-dashboard-react
* Copyright 2019 Creative Tim (https://www.creative-tim.com)
* Licensed under MIT (https://github.com/creativetimofficial/argon-dashboard-react/blob/master/LICENSE.md)

* Coded by Creative Tim

=========================================================

* The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

*/

import { useEffect, useState } from "react";
import { useSelector } from 'react-redux';
import { Alert } from "reactstrap";
import Axios from "axios";

// reactstrap components
import {
  Badge,
  Button,
  Card,
  CardHeader,
  CardBody,
  FormGroup,
  Form,
  Input,
  Container,
  Row,
  Col,
  Table,
  UncontrolledDropdown,
  DropdownToggle,
  DropdownMenu,
  DropdownItem
} from "reactstrap";
// core components
import UserHeader from "../components/Headers/UserHeader.jsx";


function Logs() {

  const userState = useSelector((state) => state.user);
  const [token, setToken] = useState(null);
  
  const [isLoading, setIsLoading] = useState(true);
  const [isSavingSettings, setIsSavingSettings] = useState(false);
  const [saveAttempted, setSaveAttempted] = useState(false);
  const [saveSuccess, setSaveSuccess] = useState(false);
  const [saveError, setSaveError] = useState("");
  
  // Settings
  const [loggingEnabled, setLoggingEnabled] = useState(true);
  const [retentionDays, setRetentionDays] = useState(90);
  const [discordLoggingEnabled, setDiscordLoggingEnabled] = useState(false);
  const [discordChannelId, setDiscordChannelId] = useState("");
  
  // Logs
  const [logs, setLogs] = useState([]);
  const [filteredLogs, setFilteredLogs] = useState([]);
  const [filterUser, setFilterUser] = useState("");
  const [filterType, setFilterType] = useState("");
  const [filterDays, setFilterDays] = useState(30);
  
  // Stats
  const [stats, setStats] = useState(null);


  // Get token from Redux or localStorage
  useEffect(() => {
    const authToken = userState?.token || window.localStorage.getItem("token");
    setToken(authToken);
  }, [userState]);

  // Load data once token is available
  useEffect(() => {
    if (token) {
      loadSettings(token);
      loadLogs(token);
      loadStats(token);
    }
  }, [token]);

  useEffect(() => {
    filterLogs();
  }, [logs, filterUser, filterType]);


  const loadSettings = (authToken = token) => {
    if (!authToken) {
      console.error("No authentication token available");
      return;
    }
    Axios.get("/api/logs/settings", {
      headers: {
        'Authorization': `Bearer ${authToken}`
      }
    })
      .then(response => {
        setLoggingEnabled(response.data.enabled);
        setRetentionDays(response.data.retentionDays);
        setDiscordLoggingEnabled(response.data.discordLoggingEnabled);
        setDiscordChannelId(response.data.discordChannelId);
        setIsLoading(false);
      })
      .catch(error => {
        setIsLoading(false);
        console.error("Error loading logging settings:", error);
      });
  };

  const loadLogs = (authToken = token) => {
    if (!authToken) {
      console.error("No authentication token available");
      return;
    }
    Axios.get(`/api/logs?days=${filterDays}`, {
      headers: {
        'Authorization': `Bearer ${authToken}`
      }
    })
      .then(response => {
        setLogs(response.data);
        setFilteredLogs(response.data);
      })
      .catch(error => {
        console.error("Error loading logs:", error);
      });
  };

  const loadStats = (authToken = token) => {
    if (!authToken) {
      console.error("No authentication token available");
      return;
    }
    Axios.get(`/api/logs/stats?days=${filterDays}`, {
      headers: {
        'Authorization': `Bearer ${authToken}`
      }
    })
      .then(response => {
        setStats(response.data);
      })
      .catch(error => {
        console.error("Error loading stats:", error);
      });
  };

  const filterLogs = () => {
    let filtered = [...logs];
    
    if (filterUser) {
      filtered = filtered.filter(log => 
        log.username.toLowerCase().includes(filterUser.toLowerCase()) ||
        log.userId.includes(filterUser)
      );
    }
    
    if (filterType) {
      filtered = filtered.filter(log => log.logType === filterType);
    }
    
    setFilteredLogs(filtered);
  };

  const saveSettings = () => {
    if (!token) {
      setSaveAttempted(true);
      setSaveError("No authentication token available. Please log in again.");
      return;
    }

    setIsSavingSettings(true);
    setSaveAttempted(true);

    Axios.post("/api/logs/settings", {
      enabled: loggingEnabled,
      retentionDays: retentionDays,
      discordLoggingEnabled: discordLoggingEnabled,
      discordChannelId: discordChannelId
    }, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    })
      .then(() => {
        setSaveSuccess(true);
        setSaveError("");
      })
      .catch(error => {
        setSaveSuccess(false);
        setSaveError(error.response?.data || "An error occurred while saving settings.");
      })
      .finally(() => {
        setIsSavingSettings(false);
      });
  };

  const getLogTypeBadge = (logType) => {
    const typeMap = {
      "MovieRequest": { color: "success", text: "Movie Request" },
      "MovieRequestDenied": { color: "danger", text: "Movie Denied" },
      "MovieIssueReported": { color: "warning", text: "Movie Issue" },
      "MovieNotificationSubscribed": { color: "info", text: "Movie Notification" },
      "TvShowRequest": { color: "success", text: "TV Request" },
      "TvShowRequestDenied": { color: "danger", text: "TV Denied" },
      "TvShowIssueReported": { color: "warning", text: "TV Issue" },
      "TvShowNotificationSubscribed": { color: "info", text: "TV Notification" }
    };
    
    const config = typeMap[logType] || { color: "secondary", text: logType };
    return <Badge color={config.color}>{config.text}</Badge>;
  };

  const formatTimestamp = (timestamp) => {
    return new Date(timestamp).toLocaleString();
  };


  return (
    <>
      <UserHeader title="Request Logs" description="View and manage request logging" />
      {/* Page content */}
      <Container className="mt--7" fluid>
        
        {/* Statistics Cards */}
        {stats && (
          <Row className="mb-4">
            <Col lg="3" md="6">
              <Card className="card-stats mb-4 mb-xl-0">
                <CardBody>
                  <Row>
                    <div className="col">
                      <span className="h2 font-weight-bold mb-0">{stats.totalRequests}</span>
                      <h5 className="card-title text-uppercase text-muted mb-0">Total Requests</h5>
                    </div>
                    <Col className="col-auto">
                      <div className="icon icon-shape bg-info text-white rounded-circle shadow">
                        <i className="fas fa-list" />
                      </div>
                    </Col>
                  </Row>
                </CardBody>
              </Card>
            </Col>
            <Col lg="3" md="6">
              <Card className="card-stats mb-4 mb-xl-0">
                <CardBody>
                  <Row>
                    <div className="col">
                      <span className="h2 font-weight-bold mb-0">{stats.movieRequests}</span>
                      <h5 className="card-title text-uppercase text-muted mb-0">Movies</h5>
                    </div>
                    <Col className="col-auto">
                      <div className="icon icon-shape bg-orange text-white rounded-circle shadow">
                        <i className="fas fa-film" />
                      </div>
                    </Col>
                  </Row>
                </CardBody>
              </Card>
            </Col>
            <Col lg="3" md="6">
              <Card className="card-stats mb-4 mb-xl-0">
                <CardBody>
                  <Row>
                    <div className="col">
                      <span className="h2 font-weight-bold mb-0">{stats.tvShowRequests}</span>
                      <h5 className="card-title text-uppercase text-muted mb-0">TV Shows</h5>
                    </div>
                    <Col className="col-auto">
                      <div className="icon icon-shape bg-blue text-white rounded-circle shadow">
                        <i className="fas fa-tv" />
                      </div>
                    </Col>
                  </Row>
                </CardBody>
              </Card>
            </Col>
            <Col lg="3" md="6">
              <Card className="card-stats mb-4 mb-xl-0">
                <CardBody>
                  <Row>
                    <div className="col">
                      <span className="h2 font-weight-bold mb-0">{stats.deniedRequests}</span>
                      <h5 className="card-title text-uppercase text-muted mb-0">Denied</h5>
                    </div>
                    <Col className="col-auto">
                      <div className="icon icon-shape bg-danger text-white rounded-circle shadow">
                        <i className="fas fa-ban" />
                      </div>
                    </Col>
                  </Row>
                </CardBody>
              </Card>
            </Col>
          </Row>
        )}

        <Row>
          <Col className="order-xl-1" xl="12">
            {/* Settings Card */}
            <Card className="bg-secondary shadow mb-4">
              <CardHeader className="bg-white border-0">
                <Row className="align-items-center">
                  <Col xs="8">
                    <h3 className="mb-0">Logging Settings</h3>
                  </Col>
                </Row>
              </CardHeader>
              <CardBody>
                <Form>
                  <div className="pl-lg-4">
                    <Row>
                      <Col md="12">
                        <FormGroup className="custom-control custom-control-alternative custom-checkbox mb-3">
                          <Input
                            className="custom-control-input"
                            id="loggingEnabled"
                            type="checkbox"
                            checked={loggingEnabled}
                            onChange={(e) => setLoggingEnabled(e.target.checked)}
                          />
                          <label
                            className="custom-control-label"
                            htmlFor="loggingEnabled">
                            Enable request logging
                          </label>
                        </FormGroup>
                      </Col>
                    </Row>
                    <Row>
                      <Col lg="6">
                        <FormGroup>
                          <label className="form-control-label">Retention (Days)</label>
                          <Input
                            className="form-control-alternative"
                            type="number"
                            value={retentionDays}
                            onChange={(e) => setRetentionDays(parseInt(e.target.value))}
                          />
                          <small className="text-muted">Number of days to keep logs (0 = forever)</small>
                        </FormGroup>
                      </Col>
                    </Row>
                    <Row>
                      <Col md="12">
                        <FormGroup className="custom-control custom-control-alternative custom-checkbox mb-3">
                          <Input
                            className="custom-control-input"
                            id="discordLoggingEnabled"
                            type="checkbox"
                            checked={discordLoggingEnabled}
                            onChange={(e) => setDiscordLoggingEnabled(e.target.checked)}
                          />
                          <label
                            className="custom-control-label"
                            htmlFor="discordLoggingEnabled">
                            Enable Discord channel logging
                          </label>
                        </FormGroup>
                      </Col>
                    </Row>
                    {discordLoggingEnabled && (
                      <Row>
                        <Col lg="6">
                          <FormGroup>
                            <label className="form-control-label">Discord Channel ID</label>
                            <Input
                              className="form-control-alternative"
                              type="text"
                              value={discordChannelId}
                              onChange={(e) => setDiscordChannelId(e.target.value)}
                              placeholder="Enter Discord channel ID"
                            />
                            <small className="text-muted">Right-click channel → Copy ID (requires Developer Mode)</small>
                          </FormGroup>
                        </Col>
                      </Row>
                    )}
                    <Row>
                      <Col>
                        <Button color="primary" onClick={saveSettings} disabled={isSavingSettings}>
                          {isSavingSettings ? "Saving..." : "Save Settings"}
                        </Button>
                      </Col>
                    </Row>
                    {saveAttempted && !isSavingSettings && (
                      <Row className="mt-3">
                        <Col>
                          {saveSuccess ? (
                            <Alert color="success">
                              <strong>Settings saved successfully!</strong>
                            </Alert>
                          ) : saveError ? (
                            <Alert color="danger">
                              <strong>Error:</strong> {saveError}
                            </Alert>
                          ) : null}
                        </Col>
                      </Row>
                    )}
                  </div>
                </Form>
              </CardBody>
            </Card>

            {/* Logs Table Card */}
            <Card className="shadow">
              <CardHeader className="border-0">
                <Row className="align-items-center">
                  <Col xs="6">
                    <h3 className="mb-0">Request Logs</h3>
                  </Col>
                  <Col xs="6" className="text-right">
                    <Button
                      color="primary"
                      size="sm"
                      onClick={() => { loadLogs(); loadStats(); }}>
                      <i className="fas fa-sync" /> Refresh
                    </Button>
                  </Col>
                </Row>
                <Row className="mt-3">
                  <Col lg="4">
                    <FormGroup>
                      <Input
                        className="form-control-alternative"
                        placeholder="Filter by user..."
                        type="text"
                        value={filterUser}
                        onChange={(e) => setFilterUser(e.target.value)}
                      />
                    </FormGroup>
                  </Col>
                  <Col lg="4">
                    <FormGroup>
                      <Input
                        className="form-control-alternative"
                        type="select"
                        value={filterType}
                        onChange={(e) => setFilterType(e.target.value)}>
                        <option value="">All Types</option>
                        <option value="MovieRequest">Movie Requests</option>
                        <option value="TvShowRequest">TV Show Requests</option>
                        <option value="MovieIssueReported">Movie Issues</option>
                        <option value="TvShowIssueReported">TV Show Issues</option>
                        <option value="MovieNotificationSubscribed">Movie Notifications</option>
                        <option value="TvShowNotificationSubscribed">TV Notifications</option>
                      </Input>
                    </FormGroup>
                  </Col>
                  <Col lg="4">
                    <FormGroup>
                      <Input
                        className="form-control-alternative"
                        type="select"
                        value={filterDays}
                        onChange={(e) => {
                          setFilterDays(parseInt(e.target.value));
                          setTimeout(() => { loadLogs(); loadStats(); }, 100);
                        }}>
                        <option value="7">Last 7 Days</option>
                        <option value="30">Last 30 Days</option>
                        <option value="90">Last 90 Days</option>
                        <option value="365">Last Year</option>
                      </Input>
                    </FormGroup>
                  </Col>
                </Row>
              </CardHeader>
              <Table className="align-items-center table-flush" responsive>
                <thead className="thead-light">
                  <tr>
                    <th scope="col">Time</th>
                    <th scope="col">User</th>
                    <th scope="col">Type</th>
                    <th scope="col">Title</th>
                    <th scope="col">Details</th>
                    <th scope="col">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredLogs.length === 0 ? (
                    <tr>
                      <td colSpan="6" className="text-center">
                        {isLoading ? "Loading logs..." : "No logs found"}
                      </td>
                    </tr>
                  ) : (
                    filteredLogs.map((log, index) => (
                      <tr key={index}>
                        <td>{formatTimestamp(log.timestamp)}</td>
                        <td>
                          <div className="d-flex flex-column">
                            <span className="mb-0 text-sm font-weight-bold">{log.username}</span>
                            <small className="text-muted">{log.userId}</small>
                          </div>
                        </td>
                        <td>{getLogTypeBadge(log.logType)}</td>
                        <td className="font-weight-bold">{log.title}</td>
                        <td>
                          <small className="text-muted">{log.details}</small>
                        </td>
                        <td>
                          {log.success ? (
                            <Badge color="success">Success</Badge>
                          ) : (
                            <span>
                              <Badge color="danger">Failed</Badge>
                              {log.reason && (
                                <div><small className="text-muted">{log.reason}</small></div>
                              )}
                            </span>
                          )}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </Table>
            </Card>
          </Col>
        </Row>
      </Container>
    </>
  );
}

export default Logs;
